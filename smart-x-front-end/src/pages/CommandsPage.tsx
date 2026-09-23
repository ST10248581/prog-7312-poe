import { useCallback, useEffect, useState } from "react";
import CommandFilterBar from "../components/commands/CommandFilterBar";
import CommandHistoryTable from "../components/commands/CommandHistoryTable";
import CommandStream from "../components/commands/CommandStream";
import OverrideConsole from "../components/commands/OverrideConsole";
import ThroughputStrip from "../components/commands/ThroughputStrip";
import StatTile from "../components/telemetry/StatTile";
import {
  EMPTY_FILTERS,
  TIME_WINDOWS,
  toCommandQuery,
} from "../components/commands/types";
import type { CommandFilters, CommandRecord } from "../components/commands/types";
import {
  dispatchCommand,
  getCommandFilterOptions,
  getCommandStream,
  getCommandSummary,
  getCommands,
} from "../services/apiService";
import type {
  CommandFilterOptions,
  CommandSummary,
  DeviceCommand,
  DispatchCommandRequest,
  PagedResult,
} from "../services/apiService";
import { formatNumber, formatTime } from "../utils/format";
// Shared widget styles — stat tiles, filter chips, panels and .data-table all
// live in the telemetry sheet. Imported explicitly so this route does not rely
// on the telemetry route having been loaded first.
import "./TelemetryPage.css";
import "./CommandsPage.css";

const PAGE_SIZE = 25;
const STREAM_SIZE = 40;

/** The backend issues and settles commands on a 2s tick, so poll to match. */
const REFRESH_MS = 3_000;

/**
 * Real-Time Command Stream and History.
 *
 * Structure mirrors the telemetry route: overview → filter → stream and act →
 * audit. Filtering, paging and dispatch are all server-side: this page holds
 * the filter state, sends it as one query to `/api/commands/*` and renders
 * exactly what comes back. Nothing is narrowed, sorted or paged in the browser.
 *
 * The stream is genuinely live rather than a static snapshot — the API's
 * dispatch simulator issues automated traffic and advances in-flight commands,
 * so a page left open sees rows arrive and pending ones settle.
 */
function CommandsPage() {
  const [filters, setFilters] = useState<CommandFilters>(EMPTY_FILTERS);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [targetNode, setTargetNode] = useState("");
  const [live, setLive] = useState(true);
  const [page, setPage] = useState(1);

  const [summary, setSummary] = useState<CommandSummary | null>(null);
  const [stream, setStream] = useState<DeviceCommand[]>([]);
  const [history, setHistory] = useState<PagedResult<DeviceCommand> | null>(null);
  const [options, setOptions] = useState<CommandFilterOptions | null>(null);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [lastRefresh, setLastRefresh] = useState<Date | null>(null);

  // `loading` covers the first paint only; live refreshes swap data in place
  // rather than flashing the panels back to a loading state.
  const loadCommands = useCallback(
    async (activeFilters: CommandFilters, activePage: number) => {
      const query = toCommandQuery(activeFilters);

      try {
        const [summaryData, streamData, historyData] = await Promise.all([
          getCommandSummary(query),
          getCommandStream(query, STREAM_SIZE),
          getCommands(query, activePage, PAGE_SIZE),
        ]);

        setSummary(summaryData);
        setStream(streamData);
        setHistory(historyData);
        setLastRefresh(new Date());
        setError(null);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Unable to reach the Smart-X API");
      } finally {
        setLoading(false);
      }
    },
    [],
  );

  // Static lookups, fetched once: filter values and the override target list.
  useEffect(() => {
    getCommandFilterOptions().then(setOptions).catch(() => undefined);
  }, []);

  // Refetch whenever the filters or the page change. The rule below sees
  // setState inside loadCommands and assumes it runs synchronously; every call
  // sits after an await, and fetching from the API is exactly the
  // external-system case the rule carves out.
  useEffect(() => {
    // oxlint-disable-next-line react/set-state-in-effect
    loadCommands(filters, page);
  }, [filters, page, loadCommands]);

  // Live polling: the real-time feedback loop.
  useEffect(() => {
    if (!live) {
      return;
    }

    const timer = window.setInterval(() => loadCommands(filters, page), REFRESH_MS);
    return () => window.clearInterval(timer);
  }, [live, filters, page, loadCommands]);

  // Memoised because the filter bar debounces the search box against it: a new
  // identity on every poll would reset that timer before it ever fired.
  const handleFilterChange = useCallback((next: CommandFilters) => {
    setFilters(next);
    // A new slice invalidates the page cursor.
    setPage(1);
  }, []);

  // Selecting a row aims the override console at that node — correcting a bad
  // command should not mean retyping its target.
  const handleSelect = (command: CommandRecord) => {
    setSelectedId(command.id);
    setTargetNode(command.nodeId);
  };

  // A queued override belongs in the stream immediately, not on the next tick.
  const handleDispatch = useCallback(
    async (request: DispatchCommandRequest) => {
      const command = await dispatchCommand(request);
      await loadCommands(filters, page);
      return command;
    },
    [loadCommands, filters, page],
  );

  const windowLabel =
    TIME_WINDOWS.find((window) => window.minutes === filters.windowMinutes)?.label ?? "1h";

  // Everything still awaiting an acknowledgement, taken from the stream the API
  // just returned rather than tracked separately.
  const pending = stream.filter(
    (command) => command.status === "Queued" || command.status === "Sent",
  );

  const totalCount = history?.totalCount ?? 0;

  /** Whether the page is already narrowed to nodes with an active alert. */
  const alertFocus = filters.alertStates.includes("Active");

  const failedTone = summary && summary.failedCount > 0 ? "danger" : "default";
  const ackTone =
    !summary || summary.acknowledgedRate >= 95
      ? "accent"
      : summary.acknowledgedRate >= 85
        ? "warning"
        : "danger";

  return (
    <div className="commands-page">
      <header className="page-head">
        <div>
          <h1 className="page-title">Real-Time Command Stream and History</h1>
          <p className="page-sub">
            Watch → filter → override → audit. Live dispatch traffic across the
            Smart-X mesh, with manual control of any node and the full command
            log behind it.
          </p>
        </div>

        <div className="page-head-actions">
          <button
            type="button"
            className={`live-toggle${live ? " active" : ""}`}
            onClick={() => setLive((value) => !value)}
          >
            <span className="live-dot" />
            {live ? "Live" : "Paused"}
          </button>
          <span className="page-refresh">
            {lastRefresh ? `Updated ${formatTime(lastRefresh.toISOString())}` : "Connecting…"}
          </span>
        </div>
      </header>

      {error && (
        <div className="page-error">
          <strong>API unreachable.</strong> {error} — start the backend with{" "}
          <code>dotnet run</code> in <code>smart-x-backend/SmartX.Api</code>.
        </div>
      )}

      {/* Overview: dispatch health before any individual command. */}
      <section className="stat-row" aria-label="Command overview">
        <StatTile
          label="Dispatch rate"
          value={summary ? summary.dispatchRate.toFixed(1) : "—"}
          unit="/min"
          tone="accent"
          hint={summary ? `${formatNumber(summary.totalCount)} in the last ${windowLabel}` : undefined}
        />

        <StatTile
          label="In flight"
          value={summary ? summary.inFlightCount : "—"}
          unit="awaiting ack"
        >
          <div className="status-breakdown">
            <span className="status-chip">{summary?.queuedCount ?? 0} queued</span>
            <span className="status-chip status-warning">
              {summary?.retryingCount ?? 0} retrying
            </span>
          </div>
        </StatTile>

        <StatTile
          label="Acknowledged"
          value={summary ? summary.acknowledgedRate.toFixed(1) : "—"}
          unit="%"
          tone={ackTone}
          progress={summary?.acknowledgedRate}
          hint={`Of settled commands, last ${windowLabel}`}
        />

        <StatTile
          label="Failed"
          value={summary ? summary.failedCount : "—"}
          tone={failedTone}
          hint={summary ? `${summary.expiredCount} expired without acknowledgement` : undefined}
        />

        <StatTile
          label="Manual overrides"
          value={summary ? summary.manualOverrideCount : "—"}
          tone="warning"
          hint={summary ? `Last ${windowLabel}, across ${summary.operatorCount} operators` : undefined}
        />

        <StatTile
          label="Round trip"
          value={summary ? summary.medianRoundTripMs : "—"}
          unit="ms"
          hint="Median, acknowledged commands"
        />

        {/* Overview → filter, in one step: the tile counts the traffic going to
            nodes that are alerting, and the button under it narrows the whole
            page to exactly that traffic. */}
        <StatTile
          label="Alerting nodes"
          value={summary ? summary.alertingNodeCount : "—"}
          tone={summary && summary.alertingCommandCount > 0 ? "warning" : "default"}
          hint={
            summary
              ? `${formatNumber(summary.alertingCommandCount)} commands to nodes with an unacknowledged alert`
              : undefined
          }
        >
          <button
            type="button"
            className={`stat-tile-action${alertFocus ? " active" : ""}`}
            onClick={() =>
              handleFilterChange({
                ...filters,
                alertStates: alertFocus
                  ? filters.alertStates.filter((state) => state !== "Active")
                  : [...filters.alertStates, "Active"],
              })
            }
          >
            {alertFocus ? "Showing only these" : "Show only these"}
          </button>
        </StatTile>
      </section>

      {/* Visualises the incoming stream as a rate, so a burst or a stall is
          visible without reading individual rows. */}
      <section className="throughput-panel" aria-label="Dispatch throughput">
        <header className="panel-head">
          <h2>Dispatch throughput</h2>
          <span className="panel-head-count">commands per minute</span>
        </header>
        <ThroughputStrip values={summary?.throughput ?? []} windowLabel={windowLabel} />
      </section>

      <CommandFilterBar
        options={options}
        filters={filters}
        resultCount={stream.length}
        totalCount={totalCount}
        categoryCounts={summary?.categoryCounts}
        onChange={handleFilterChange}
      />

      <div className="commands-grid">
        <CommandStream
          commands={stream}
          selectedId={selectedId}
          live={live}
          loading={loading}
          onSelect={handleSelect}
        />

        <OverrideConsole
          options={options}
          targetNode={targetNode}
          pending={pending}
          onTargetChange={setTargetNode}
          onDispatch={handleDispatch}
        />
      </div>

      <CommandHistoryTable
        commands={history?.items ?? []}
        page={history?.page ?? page}
        pageSize={history?.pageSize ?? PAGE_SIZE}
        totalCount={totalCount}
        onPageChange={setPage}
      />
    </div>
  );
}

export default CommandsPage;
