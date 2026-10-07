import { useCallback, useEffect, useRef, useState } from "react";
import CommandFilterBar from "../components/commands/CommandFilterBar";
import CommandHistoryTable from "../components/commands/CommandHistoryTable";
import CommandStream from "../components/commands/CommandStream";
import IngestPipeline from "../components/commands/IngestPipeline";
import LearningPanel from "../components/commands/LearningPanel";
import LiveDevicePanel from "../components/commands/LiveDevicePanel";
import NodeTimeline from "../components/commands/NodeTimeline";
import OverrideConsole from "../components/commands/OverrideConsole";
import type { OverrideDraft } from "../components/commands/OverrideConsole";
import SuggestedActions from "../components/commands/SuggestedActions";
import ThroughputStrip from "../components/commands/ThroughputStrip";
import StatTile from "../components/telemetry/StatTile";
import { useToast } from "../components/toast/toastContext";
import {
  EMPTY_FILTERS,
  TIME_WINDOWS,
  hasActiveFilters,
  toCommandQuery,
  toDeviceQuery,
} from "../components/commands/types";
import type { CommandFilters, CommandRecord } from "../components/commands/types";
import {
  ApiError,
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
  redoLastUndo,
  resetLearning,
  sendSuggestionFeedback,
  undoLastOverride,
} from "../services/apiService";
import type {
  CommandFilterOptions,
  CommandStatus,
  CommandSummary,
  CommandType,
  DeviceCommand,
  DispatchCommandRequest,
  InsightsResponse,
  LiveDeviceResult,
  NodeAlertState,
  NodeTimeline as NodeTimelineData,
  OverrideHistory,
  PagedResult,
  PipelineStatus,
  SensorCategory,
  StreamPacketRequest,
  SuggestedAction,
} from "../services/apiService";
import { isAbortError, usePolling } from "../hooks/usePolling";
import { usePersistentState } from "../state/appState";
import { formatNumber, formatTime, humanise } from "../utils/format";
// Shared widget styles — stat tiles, filter chips, panels and .data-table all
// live in the telemetry sheet. Imported explicitly so this route does not rely
// on the telemetry route having been loaded first.
import "./TelemetryPage.css";
import "./CommandsPage.css";

const PAGE_SIZE = 25;
const STREAM_SIZE = 40;

/** The backend issues and settles commands on a 2s tick, so poll to match. */
const REFRESH_MS = 3_000;

/** A learned suggestion at least this confident is announced with a toast, before the operator looks for it. */
const PROACTIVE_CONFIDENCE = 0.75;

const EMPTY_HISTORY: OverrideHistory = { undo: [], redo: [] };

/** Filter values switched on between two filter states, as "facet:value" — the query history the engine learns from. */
function newlyEnabledFilters(previous: CommandFilters, next: CommandFilters): string[] {
  const added: string[] = [];
  const diff = (facet: string, before: readonly string[], after: readonly string[]) =>
    after.filter((value) => !before.includes(value)).forEach((value) => added.push(`${facet}:${value}`));

  diff("sensorCategory", previous.sensorCategories, next.sensorCategories);
  diff("alertState", previous.alertStates, next.alertStates);
  diff("status", previous.statuses, next.statuses);
  diff("commandType", previous.commandTypes, next.commandTypes);
  if (next.zone && next.zone !== previous.zone) {
    added.push(`zone:${next.zone}`);
  }
  return added;
}

/** Applies a learned filter suggestion to the current filters. */
function withFilter(filters: CommandFilters, facet: string, value: string): CommandFilters {
  const addTo = <T extends string>(list: T[]) => (list.includes(value as T) ? list : [...list, value as T]);

  switch (facet) {
    case "zone":
      return { ...filters, zone: value };
    case "sensorCategory":
      return { ...filters, sensorCategories: addTo<SensorCategory>(filters.sensorCategories) };
    case "alertState":
      return { ...filters, alertStates: addTo<NodeAlertState>(filters.alertStates) };
    case "status":
      return { ...filters, statuses: addTo<CommandStatus>(filters.statuses) };
    case "commandType":
      return { ...filters, commandTypes: addTo<CommandType>(filters.commandTypes) };
    default:
      return filters;
  }
}

/** An unreachable API is announced app-wide by ApiStatusBanner; only the API's own rejections are shown here. */
function describeError(err: unknown, fallback: string): string | null {
  if (err instanceof ApiError && err.isUnreachable) return null;
  return err instanceof Error ? err.message : fallback;
}

/**
 * Real-Time Command Stream and History.
 *
 * Structure mirrors the telemetry route: overview → filter → stream and act →
 * audit. Filtering, paging and dispatch are all server-side: this page holds
 * the filter state, sends it as one query to `/api/commands/*` and renders
 * exactly what comes back. Nothing is narrowed, sorted or paged in the browser.
 *
 * State that should survive moving to another module — filters, page, live
 * toggle, target node, timeline window, dismissed suggestions and the last data
 * shown — is held in the app state store, so returning here restores the page
 * exactly. Polling never overlaps, is cancelled when the page unmounts, and
 * rests while the browser tab is hidden.
 */
function CommandsPage() {
  const toast = useToast();

  /* ---------- Operator choices: kept across navigation and reloads ---------- */
  const [filters, setFilters] = usePersistentState<CommandFilters>("commands.filters", EMPTY_FILTERS, { session: true });
  const [page, setPage] = usePersistentState("commands.page", 1, { session: true });
  const [live, setLive] = usePersistentState("commands.live", true, { session: true });
  const [targetNode, setTargetNode] = usePersistentState("commands.targetNode", "", { session: true });
  const [timelineMinutes, setTimelineMinutes] = usePersistentState("commands.timelineMinutes", 60, { session: true });
  const [dismissed, setDismissed] = usePersistentState<string[]>("commands.dismissed", [], { session: true });
  const [selectedId, setSelectedId] = usePersistentState<string | null>("commands.selectedId", null);

  /* ---------- Last data shown: kept across navigation, refreshed on return ---------- */
  const [summary, setSummary] = usePersistentState<CommandSummary | null>("commands.summary", null);
  const [stream, setStream] = usePersistentState<DeviceCommand[]>("commands.stream", []);
  const [history, setHistory] = usePersistentState<PagedResult<DeviceCommand> | null>("commands.history", null);
  const [options, setOptions] = usePersistentState<CommandFilterOptions | null>("commands.options", null);
  const [devices, setDevices] = usePersistentState<LiveDeviceResult | null>("commands.devices", null);
  const [pipeline, setPipeline] = usePersistentState<PipelineStatus | null>("commands.pipeline", null);
  const [overrides, setOverrides] = usePersistentState<OverrideHistory>("commands.overrides", EMPTY_HISTORY);
  const [insights, setInsights] = usePersistentState<InsightsResponse | null>("commands.insights", null);
  const [timeline, setTimeline] = usePersistentState<NodeTimelineData | null>("commands.timeline", null);
  const [lastRefresh, setLastRefresh] = usePersistentState<Date | null>("commands.lastRefresh", null);

  /** The disconnected set as of the last poll; the API diffs the current one against it. */
  const [knownDisconnected, setKnownDisconnected] = usePersistentState<string[] | undefined>(
    "commands.knownDisconnected",
    undefined
  );
  /** Suggestions already announced with a toast this session. */
  const [notified, setNotified] = usePersistentState<string[]>("commands.notified", [], { session: true });

  const [devicesError, setDevicesError] = useState<string | null>(null);
  const [timelineError, setTimelineError] = useState<string | null>(null);
  const [draft, setDraft] = useState<OverrideDraft | null>(null);
  const [loading, setLoading] = useState(summary === null);
  const [error, setError] = useState<string | null>(null);

  const consoleRef = useRef<HTMLDivElement>(null);

  /* ---------- Loaders: each takes the poll's AbortSignal ---------- */

  const loadCommands = useCallback(
    async (activeFilters: CommandFilters, activePage: number, signal?: AbortSignal) => {
      const query = toCommandQuery(activeFilters);

      try {
        const [summaryData, streamData, historyData] = await Promise.all([
          getCommandSummary(query, signal),
          getCommandStream(query, STREAM_SIZE, signal),
          getCommands(query, activePage, PAGE_SIZE, signal),
        ]);
        if (signal?.aborted) return;

        setSummary(summaryData);
        setStream(streamData);
        setHistory(historyData);
        setLastRefresh(new Date());
        setError(null);
      } catch (err) {
        if (isAbortError(err)) return;
        setError(describeError(err, "Request failed."));
      } finally {
        setLoading(false);
      }
    },
    [setSummary, setStream, setHistory, setLastRefresh]
  );

  // The live device panel takes the device half of the same filter. It fails
  // on its own, so a device hiccup never blanks the command stream.
  const loadDevices = useCallback(
    async (activeFilters: CommandFilters, signal?: AbortSignal) => {
      try {
        const data = await getLiveDevices(toDeviceQuery(activeFilters), signal);
        if (signal?.aborted) return;
        setDevices(data);
        setDevicesError(null);
      } catch (err) {
        if (isAbortError(err)) return;
        setDevicesError(err instanceof Error ? err.message : "Unable to load devices.");
      }
    },
    [setDevices]
  );

  /** Announces a confident learned suggestion before the operator goes looking for it. */
  const announce = useRef<(data: InsightsResponse) => void>(() => undefined);

  // The engine panels are independent of the command filter, and each fails
  // on its own: an insights hiccup must not blank the intake panel.
  const loadEngine = useCallback(
    async (signal?: AbortSignal) => {
      const [pipelineData, overrideData, insightData, optionData] = await Promise.allSettled([
        getPipelineStatus(knownDisconnected, signal),
        getOverrideHistory(signal),
        getInsights(undefined, signal),
        // Re-read, not fetched once: a node that drops off the mesh has to leave
        // the target list, and one that comes back has to rejoin it.
        getCommandFilterOptions(signal),
      ]);
      if (signal?.aborted) return;

      if (pipelineData.status === "fulfilled") {
        setPipeline(pipelineData.value);
        setKnownDisconnected(pipelineData.value.disconnectedNodes.map((node) => node.nodeId));
      }
      if (overrideData.status === "fulfilled") setOverrides(overrideData.value);
      if (insightData.status === "fulfilled") {
        setInsights(insightData.value);
        announce.current(insightData.value);
      }
      if (optionData.status === "fulfilled") setOptions(optionData.value);
    },
    [knownDisconnected, setPipeline, setKnownDisconnected, setOverrides, setInsights, setOptions]
  );

  const loadTimeline = useCallback(
    async (nodeId: string, minutes: number, signal?: AbortSignal) => {
      if (!nodeId) {
        setTimeline(null);
        return;
      }

      try {
        const data = await getNodeTimeline(nodeId, minutes, 120, signal);
        if (signal?.aborted) return;
        setTimeline(data);
        setTimelineError(null);
      } catch (err) {
        if (isAbortError(err)) return;
        setTimelineError(err instanceof Error ? err.message : "Unable to load the node's timeline.");
      }
    },
    [setTimeline]
  );

  /* ---------- Polling: immediate on change, then every REFRESH_MS while live ---------- */

  usePolling((signal) => loadCommands(filters, page, signal), {
    intervalMs: REFRESH_MS,
    live,
    key: JSON.stringify([filters, page]),
  });

  usePolling((signal) => loadDevices(filters, signal), {
    intervalMs: REFRESH_MS,
    live,
    key: JSON.stringify(filters),
  });

  usePolling((signal) => loadEngine(signal), { intervalMs: REFRESH_MS, live });

  usePolling((signal) => loadTimeline(targetNode, timelineMinutes, signal), {
    intervalMs: REFRESH_MS,
    live,
    key: `${targetNode}|${timelineMinutes}`,
  });

  const refreshAfterAction = useCallback(
    () => Promise.all([loadCommands(filters, page), loadEngine()]),
    [loadCommands, loadEngine, filters, page]
  );

  /* ---------- Filters: every value switched on is query history the engine learns from ---------- */

  // Memoised because the filter bar debounces the search box against it: a new
  // identity on every poll would reset that timer before it ever fired.
  const handleFilterChange = useCallback(
    (next: CommandFilters) => {
      // A committed search, and each filter value newly switched on, is
      // reported once — the same state arriving again is not news.
      const term = next.search.trim();
      if (term !== "" && term !== filters.search.trim()) {
        recordActivity("Search", term);
      }
      newlyEnabledFilters(filters, next).forEach((facet) => recordActivity("Filter", facet));

      setFilters(next);
      // A new slice invalidates the page cursor.
      setPage(1);
    },
    [filters, setFilters, setPage]
  );

  /** Points the console and the timeline at a node, and tells the engine it was inspected. */
  const focusNode = useCallback(
    (nodeId: string) => {
      setTargetNode(nodeId);
      recordActivity("SelectNode", nodeId);
    },
    [setTargetNode]
  );

  // Selecting a row aims the override console at that node — correcting a bad
  // command should not mean retyping its target.
  const handleSelect = (command: CommandRecord) => {
    setSelectedId(command.id);
    focusNode(command.nodeId);
  };

  /* ---------- Overrides: dispatch, undo, redo ---------- */

  // A queued override belongs in the stream immediately, not on the next tick.
  const handleDispatch = useCallback(
    async (request: DispatchCommandRequest) => {
      const command = await dispatchCommand(request);
      await refreshAfterAction();
      return command;
    },
    [refreshAfterAction]
  );

  const handleUndo = useCallback(
    async (expectedCommandId: string) => {
      const result = await undoLastOverride(expectedCommandId);
      await refreshAfterAction();
      return result;
    },
    [refreshAfterAction]
  );

  const handleRedo = useCallback(
    async (expectedCommandId: string) => {
      const result = await redoLastUndo(expectedCommandId);
      await refreshAfterAction();
      return result;
    },
    [refreshAfterAction]
  );

  /* ---------- Suggestions: apply, edit, dismiss, and feedback ---------- */

  /** Fills the console with a command suggestion so it can be adjusted before sending. */
  const handlePrepare = useCallback(
    (suggestion: SuggestedAction) => {
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
    },
    [setTargetNode]
  );

  /**
   * One-click apply. Does what the suggestion says, then tells the engine it
   * was taken, which lifts it in later rankings. A sent command is on the undo
   * stack straight away, and the toast offers to undo it.
   */
  const handleApply = useCallback(
    async (suggestion: SuggestedAction) => {
      sendSuggestionFeedback(suggestion.id, "Applied");

      if (suggestion.commandType && suggestion.nodeId) {
        try {
          const command = await handleDispatch({
            nodeId: suggestion.nodeId,
            commandType: suggestion.commandType,
            parameters: suggestion.parameters ?? "",
            priority: suggestion.priority ?? "High",
            dryRun: suggestion.dryRun,
          });
          setTargetNode(suggestion.nodeId);
          toast.show({
            tone: "success",
            title: `${humanise(command.commandType)} queued for ${command.nodeId}`,
            message: command.isDryRun ? "Logged as a dry run: the node is offline." : `${command.parameters} · on the undo stack`,
            actions: command.isDryRun ? undefined : [{ label: "Undo", onClick: () => void handleUndo(command.id).catch(() => undefined) }],
          });
        } catch (err) {
          toast.show({
            tone: "error",
            title: "Suggestion not applied",
            message: err instanceof Error ? err.message : "The command was rejected.",
          });
        }
        return;
      }

      if (suggestion.searchTerm) {
        handleFilterChange({ ...filters, search: suggestion.searchTerm });
      } else if (suggestion.filterFacet && suggestion.filterValue) {
        handleFilterChange(withFilter(filters, suggestion.filterFacet, suggestion.filterValue));
      } else if (suggestion.nodeId) {
        focusNode(suggestion.nodeId);
      }
      void loadEngine();
    },
    [handleDispatch, handleUndo, handleFilterChange, filters, focusNode, loadEngine, setTargetNode, toast]
  );

  const handleDismiss = useCallback(
    (suggestion: SuggestedAction) => {
      setDismissed((current) => (current.includes(suggestion.id) ? current : [...current, suggestion.id]));
      sendSuggestionFeedback(suggestion.id, "Dismissed").then(() => loadEngine());
    },
    [setDismissed, loadEngine]
  );

  const handleResetLearning = useCallback(async () => {
    try {
      await resetLearning();
      setDismissed([]);
      setNotified([]);
      await loadEngine();
      toast.show({ tone: "info", title: "Learning reset", message: "The engine now starts from an empty history." });
    } catch (err) {
      toast.show({ tone: "error", title: "Could not reset", message: err instanceof Error ? err.message : "" });
    }
  }, [setDismissed, setNotified, loadEngine, toast]);

  // Proactive alert: the first time a learned suggestion is confident enough,
  // say so with a toast, wherever the operator is looking on the page. Kept in
  // a ref (refreshed every render) so the engine loader always sees the latest
  // notified and dismissed lists without being recreated.
  const announceSuggestion = (data: InsightsResponse) => {
    const fresh = data.suggestions.find(
      (suggestion) =>
        suggestion.kind !== "ProblemDevice" &&
        suggestion.confidence >= PROACTIVE_CONFIDENCE &&
        !notified.includes(suggestion.id) &&
        !dismissed.includes(suggestion.id)
    );
    if (!fresh) return;

    setNotified((current) => [...current, fresh.id]);
    toast.show({
      tone: "insight",
      title: `Likely next: ${fresh.title}`,
      message: `${Math.round(fresh.confidence * 100)}% confident. Because ${fresh.reason.charAt(0).toLowerCase()}${fresh.reason.slice(1)}`,
      durationMs: 12_000,
      actions: [
        { label: "Apply", onClick: () => void handleApply(fresh) },
        { label: "Dismiss", onClick: () => handleDismiss(fresh) },
      ],
    });
  };

  useEffect(() => {
    announce.current = announceSuggestion;
  });

  /* ---------- Intake simulations ---------- */

  const powerNode = useCallback(() => {
    const nodes = options?.nodes ?? [];
    const node = [targetNode, ...nodes].find((nodeId) => /^(PWR|ACT|NET)-/.test(nodeId)) ?? nodes[0];
    if (!node) {
      throw new Error("No reachable node to send packets for.");
    }
    return node;
  }, [options, targetNode]);

  const sendPackets = useCallback(
    async (routineCount: number) => {
      const node = powerNode();
      const routine: StreamPacketRequest[] = Array.from({ length: routineCount }, () => ({
        nodeId: node,
        readingType: "Power",
        value: 1.5 + Math.random(),
      }));
      const spike: StreamPacketRequest = { nodeId: node, readingType: "Power", value: 12 };

      const result = await ingestStreamPackets([...routine, spike]);
      await Promise.all([refreshAfterAction(), loadDevices(filters)]);
      return result;
    },
    [powerNode, refreshAfterAction, loadDevices, filters]
  );

  /**
   * Demonstrates the two lanes: a routine burst joins the FIFO queue behind
   * whatever is already there, while the spike posted after it is processed
   * before the response even comes back.
   */
  const handleSimulateSpike = useCallback(() => sendPackets(20), [sendPackets]);

  /** Demonstrates backpressure: more routine packets than the queue holds, so the oldest are shed. */
  const handleSimulateFlood = useCallback(
    () => sendPackets((pipeline?.standardQueueCapacity ?? 1_000) + 200),
    [sendPackets, pipeline]
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
            {lastRefresh ? `Updated ${formatTime(new Date(lastRefresh).toISOString())}` : "Connecting…"}
          </span>
        </div>
      </header>

      {error && (
        <div className="page-error" role="alert">
          <strong>The command stream could not refresh.</strong> {error}
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
        <div className="insights-main">
          <SuggestedActions
            insights={insights}
            dismissed={dismissed}
            onApply={handleApply}
            onPrepare={handlePrepare}
            onDismiss={handleDismiss}
            onInspect={focusNode}
          />

          <LearningPanel insights={insights} onReset={handleResetLearning} />
        </div>

        <IngestPipeline
          pipeline={pipeline}
          onSimulateSpike={handleSimulateSpike}
          onSimulateFlood={handleSimulateFlood}
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

      {/* Scroll target for "Edit first" on a suggestion. A sibling rather than a
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
            <NodeTimeline
              nodeId={targetNode}
              timeline={timeline?.nodeId === targetNode ? timeline : null}
              error={timelineError}
              windowMinutes={timelineMinutes}
              onWindowChange={setTimelineMinutes}
            />
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
          onRedo={handleRedo}
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
