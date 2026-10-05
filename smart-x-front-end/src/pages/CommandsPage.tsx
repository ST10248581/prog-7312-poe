import { useCallback, useEffect, useRef, useState } from "react";
import CommandFilterBar from "../components/commands/CommandFilterBar";
import CommandHistoryTable from "../components/commands/CommandHistoryTable";
import CommandStream from "../components/commands/CommandStream";
import IngestPipeline from "../components/commands/IngestPipeline";
import LiveDevicePanel from "../components/commands/LiveDevicePanel";
import NodeTimeline from "../components/commands/NodeTimeline";
import OverrideConsole from "../components/commands/OverrideConsole";
import type { OverrideDraft } from "../components/commands/OverrideConsole";
import SuggestedActions from "../components/commands/SuggestedActions";
import ThroughputStrip from "../components/commands/ThroughputStrip";
import StatTile from "../components/telemetry/StatTile";
import {
  EMPTY_FILTERS,
  TIME_WINDOWS,
  hasActiveFilters,
  toCommandQuery,
  toDeviceQuery,
} from "../components/commands/types";
import type { CommandFilters, CommandRecord } from "../components/commands/types";
import {
  dispatchCommand,
  getCommandFilterOptions,
  getCommandStream,
  getCommandSummary,
  getCommands,
  getInsights,
  getLiveDevices,
  getNodeTimeline,
  getOverrideHistory,
  getPipelineStatus,
  ingestStreamPackets,
  recordActivity,
  undoLastOverride,
} from "../services/apiService";
import type {
  CommandFilterOptions,
  CommandSummary,
  DeviceCommand,
  DispatchCommandRequest,
  InsightsResponse,
  LiveDeviceResult,
  NodeTimeline as NodeTimelineData,
  OverrideHistoryEntry,
  PagedResult,
  PipelineStatus,
  StreamPacketRequest,
  SuggestedAction,
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
  const [devices, setDevices] = useState<LiveDeviceResult | null>(null);
  const [devicesError, setDevicesError] = useState<string | null>(null);

  // The command engine's side of the page: intake, undo stack, insights and
  // the selected node's timeline.
  const [pipeline, setPipeline] = useState<PipelineStatus | null>(null);
  const [overrides, setOverrides] = useState<OverrideHistoryEntry[]>([]);
  const [insights, setInsights] = useState<InsightsResponse | null>(null);
  const [timeline, setTimeline] = useState<NodeTimelineData | null>(null);
  const [timelineError, setTimelineError] = useState<string | null>(null);
  const [draft, setDraft] = useState<OverrideDraft | null>(null);

  /** The last search sent to the action engine, so a poll never re-reports it. */
  const reportedSearch = useRef("");
  const consoleRef = useRef<HTMLDivElement>(null);

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

  // The live device panel takes the device half of the same filter. It fails
  // on its own, so a device hiccup never blanks the command stream.
  const loadDevices = useCallback(async (activeFilters: CommandFilters) => {
    try {
      setDevices(await getLiveDevices(toDeviceQuery(activeFilters)));
      setDevicesError(null);
    } catch (err) {
      setDevicesError(err instanceof Error ? err.message : "Unable to load devices.");
    }
  }, []);

  // Refetch whenever the filters or the page change. The rule below sees
  // setState inside loadCommands and assumes it runs synchronously; every call
  // sits after an await, and fetching from the API is exactly the
  // external-system case the rule carves out.
  useEffect(() => {
    // oxlint-disable-next-line react/set-state-in-effect
    loadCommands(filters, page);
  }, [filters, page, loadCommands]);

  useEffect(() => {
    // oxlint-disable-next-line react/set-state-in-effect
    loadDevices(filters);
  }, [filters, loadDevices]);

  // Live polling: the real-time feedback loop.
  useEffect(() => {
    if (!live) {
      return;
    }

    const timer = window.setInterval(() => {
      loadCommands(filters, page);
      loadDevices(filters);
    }, REFRESH_MS);
    return () => window.clearInterval(timer);
  }, [live, filters, page, loadCommands, loadDevices]);

  // The engine panels are independent of the command filter, and each fails
  // on its own: an insights hiccup must not blank the intake panel.
  const loadEngine = useCallback(async () => {
    const [pipelineData, overrideData, insightData, optionData] = await Promise.allSettled([
      getPipelineStatus(),
      getOverrideHistory(),
      getInsights(),
      // Re-read, not fetched once: a node that drops off the mesh has to leave
      // the target list, and one that comes back has to rejoin it.
      getCommandFilterOptions(),
    ]);

    if (pipelineData.status === "fulfilled") setPipeline(pipelineData.value);
    if (overrideData.status === "fulfilled") setOverrides(overrideData.value);
    if (insightData.status === "fulfilled") setInsights(insightData.value);
    if (optionData.status === "fulfilled") setOptions(optionData.value);
  }, []);

  const loadTimeline = useCallback(async (nodeId: string) => {
    if (!nodeId) {
      setTimeline(null);
      return;
    }

    try {
      const data = await getNodeTimeline(nodeId);
      setTimeline(data);
      setTimelineError(null);
    } catch (err) {
      setTimelineError(err instanceof Error ? err.message : "Unable to load the node's timeline.");
    }
  }, []);

  useEffect(() => {
    // oxlint-disable-next-line react/set-state-in-effect
    loadEngine();
  }, [loadEngine]);

  useEffect(() => {
    // oxlint-disable-next-line react/set-state-in-effect
    loadTimeline(targetNode);
  }, [targetNode, loadTimeline]);

  useEffect(() => {
    if (!live) {
      return;
    }

    const timer = window.setInterval(() => {
      loadEngine();
      loadTimeline(targetNode);
    }, REFRESH_MS);
    return () => window.clearInterval(timer);
  }, [live, targetNode, loadEngine, loadTimeline]);

  // Memoised because the filter bar debounces the search box against it: a new
  // identity on every poll would reset that timer before it ever fired.
  const handleFilterChange = useCallback((next: CommandFilters) => {
    setFilters(next);
    // A new slice invalidates the page cursor.
    setPage(1);

    // A committed search is something the action engine learns from. Only the
    // change is reported; the same term arriving again on a poll is not news.
    const term = next.search.trim();
    if (term !== "" && term !== reportedSearch.current) {
      recordActivity("Search", term);
    }
    reportedSearch.current = term;
  }, []);

  /** Points the console and the timeline at a node, and tells the engine it was inspected. */
  const focusNode = useCallback((nodeId: string) => {
    setTargetNode(nodeId);
    recordActivity("SelectNode", nodeId);
  }, []);

  // Selecting a row aims the override console at that node — correcting a bad
  // command should not mean retyping its target.
  const handleSelect = (command: CommandRecord) => {
    setSelectedId(command.id);
    focusNode(command.nodeId);
  };

  // A queued override belongs in the stream immediately, not on the next tick.
  const handleDispatch = useCallback(
    async (request: DispatchCommandRequest) => {
      const command = await dispatchCommand(request);
      await Promise.all([loadCommands(filters, page), loadEngine()]);
      return command;
    },
    [loadCommands, loadEngine, filters, page],
  );

  const handleUndo = useCallback(async () => {
    const result = await undoLastOverride();
    await Promise.all([loadCommands(filters, page), loadEngine()]);
    return result;
  }, [loadCommands, loadEngine, filters, page]);

  /** A suggestion only fills the console; the operator still confirms it. */
  const handlePrepare = useCallback((suggestion: SuggestedAction) => {
    if (!suggestion.nodeId || !suggestion.commandType) {
      return;
    }

    setTargetNode(suggestion.nodeId);
    setDraft({
      key: `${suggestion.id}:${Date.now()}`,
      commandType: suggestion.commandType,
      parameters: suggestion.parameters ?? "",
      priority: suggestion.priority ?? "High",
      dryRun: suggestion.dryRun,
      source: suggestion.reason,
    });
    consoleRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });
  }, []);

  const handleSuggestedSearch = useCallback(
    (term: string) => handleFilterChange({ ...filters, search: term }),
    [filters, handleFilterChange],
  );

  /**
   * Demonstrates the two lanes: a routine burst joins the FIFO queue behind
   * whatever is already there, while the spike posted after it is processed
   * before the response even comes back.
   */
  const handleSimulateSpike = useCallback(async () => {
    const nodes = options?.nodes ?? [];
    const powerNode =
      [targetNode, ...nodes].find((nodeId) => /^(PWR|ACT|NET)-/.test(nodeId)) ?? nodes[0];

    if (!powerNode) {
      throw new Error("No reachable node to send packets for.");
    }

    const routine: StreamPacketRequest[] = Array.from({ length: 20 }, () => ({
      nodeId: powerNode,
      readingType: "Power",
      value: 1.5 + Math.random(),
    }));
    const spike: StreamPacketRequest = { nodeId: powerNode, readingType: "Power", value: 12 };

    const result = await ingestStreamPackets([...routine, spike]);
    await Promise.all([loadCommands(filters, page), loadEngine(), loadDevices(filters)]);
    return result;
  }, [options, targetNode, loadCommands, loadEngine, loadDevices, filters, page]);

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

      {/* Predict → act: the engine's suggestions sit above the filter, so the
          next move is on screen before the operator has to search for it. */}
      <div className="insights-grid">
        <SuggestedActions
          insights={insights}
          onPrepare={handlePrepare}
          onSearch={handleSuggestedSearch}
          onInspect={focusNode}
        />

        <IngestPipeline
          pipeline={pipeline}
          onSimulateSpike={handleSimulateSpike}
          onInspect={focusNode}
        />
      </div>

      <CommandFilterBar
        options={options}
        filters={filters}
        resultCount={stream.length}
        totalCount={totalCount}
        categoryCounts={summary?.categoryCounts}
        deviceCounts={
          devices
            ? { categories: devices.categoryCounts, alertStates: devices.alertStateCounts }
            : undefined
        }
        onChange={handleFilterChange}
      />

      {/* Watch: the incoming sensor data itself, every matching device with its
          latest readings, before the commands sent to them. */}
      <LiveDevicePanel
        result={devices}
        error={devicesError}
        selectedNode={targetNode}
        filtered={hasActiveFilters(filters)}
        onInspect={focusNode}
      />

      {/* Scroll target for "Prepare" on a suggestion. A sibling rather than a
          wrapper, which would stop the console sticking. */}
      <div ref={consoleRef} className="commands-anchor" aria-hidden="true" />

      <div className="commands-grid">
        <div className="commands-main">
          <CommandStream
            commands={stream}
            selectedId={selectedId}
            live={live}
            loading={loading}
            onSelect={handleSelect}
          />

          {targetNode && (
            <NodeTimeline nodeId={targetNode} timeline={timeline} error={timelineError} />
          )}
        </div>

        <OverrideConsole
          options={options}
          targetNode={targetNode}
          pending={pending}
          history={overrides}
          draft={draft}
          onTargetChange={setTargetNode}
          onDispatch={handleDispatch}
          onUndo={handleUndo}
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
