import { useState } from "react";
import type { FormEvent } from "react";
import MeshCanvas from "../components/topology/MeshCanvas";
import type { ViewMode } from "../components/topology/MeshCanvas";
import StatTile from "../components/telemetry/StatTile";
import { useToast } from "../components/toast/toastContext";
import {
  ApiError,
  clearNodeFailures,
  getTelemetryHistory,
  getTopology,
  recomputeTopology,
  scanConnectivity,
  setNodeFailure,
  traceRoute,
} from "../services/apiService";
import type {
  ConnectivityScan,
  HistoryEntryStatus,
  HistorySort,
  MeshTopology,
  ReadingType,
  RoutePath,
  RouteTrace,
  ScanNode,
  ScanNodeStatus,
  TelemetryHistory,
  TraversalAlgorithm,
} from "../services/apiService";
import { isAbortError, usePolling } from "../hooks/usePolling";
import { usePersistentState } from "../state/appState";
import { formatNumber, formatTime, humanise } from "../utils/format";
import "./TelemetryPage.css";
import "./TopologyPage.css";

/** The mesh re-routes on a 3s server tick; poll a little slower than that. */
const REFRESH_MS = 5_000;
const HISTORY_REFRESH_MS = 10_000;
const HISTORY_PAGE_SIZE = 25;
const MIN_ZOOM = 1;
const MAX_ZOOM = 4;

const VIEW_MODES: { id: ViewMode; label: string }[] = [
  { id: "mesh", label: "Full mesh" },
  { id: "mst", label: "Minimum spanning tree" },
  { id: "route", label: "Traced route" },
  { id: "scan", label: "BFS / DFS scan" },
];

/** hours = 0 selects an exact from/to range instead of "the last N hours". */
const CUSTOM_RANGE = 0;

const TIME_RANGES = [
  { hours: 1, label: "Last hour" },
  { hours: 24, label: "Last 24 hours" },
  { hours: 168, label: "Last 7 days" },
  { hours: CUSTOM_RANGE, label: "Custom range…" },
];

const STATUS_BADGE: Record<HistoryEntryStatus, string> = {
  Normal: "badge badge-ok",
  Breach: "badge badge-danger",
  Suspect: "badge badge-warning",
  Bad: "badge badge-danger",
};

const SCAN_BADGE: Record<ScanNodeStatus, { className: string; label: string }> = {
  Connected: { className: "badge badge-ok", label: "Connected" },
  Down: { className: "badge badge-danger", label: "Down" },
  CutOff: { className: "badge badge-danger", label: "Cut off" },
  OtherGateway: { className: "badge badge-warning", label: "Other gateway" },
};

const ALGORITHMS: { id: TraversalAlgorithm; label: string }[] = [
  { id: "Bfs", label: "BFS" },
  { id: "Dfs", label: "DFS" },
];

/** A Date as a `datetime-local` input value, in the operator's own time zone. */
function toLocalInput(date: Date): string {
  const pad = (value: number) => String(value).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

/** The most recent 14:00–16:00 that has already ended: today's if it is past 16:00, else yesterday's. */
function defaultCustomRange(): { from: string; to: string } {
  const to = new Date();
  if (to.getHours() < 16) to.setDate(to.getDate() - 1);
  to.setHours(16, 0, 0, 0);
  const from = new Date(to);
  from.setHours(14);
  return { from: toLocalInput(from), to: toLocalInput(to) };
}

function describeScanNode(node: ScanNode): string {
  switch (node.status) {
    case "Down":
      return `${humanise(node.offlineReason)} · ${node.strandedDependants} node(s) relayed through it` +
        (node.alternateRelays.length > 0 ? ` · traffic can hop via ${node.alternateRelays.join(", ")}` : " · no live neighbour to take over");
    case "CutOff":
      return node.blockedBy.length > 0 ? `No live path — blocked by ${node.blockedBy.join(", ")}` : "No live path to a gateway";
    case "OtherGateway":
      return "Reachable, but only through another gateway";
    default:
      return node.depth === 0 ? "Root" : "";
  }
}

/** An unreachable API is announced app-wide by ApiStatusBanner; only the API's own rejections are shown here. */
function describeError(err: unknown, fallback: string): string | null {
  if (err instanceof ApiError && err.isUnreachable) return null;
  return err instanceof Error ? err.message : fallback;
}

/** Readings land every few seconds on the live log, so the history shows seconds too. */
function formatStamp(iso: string): string {
  return new Date(iso).toLocaleString(undefined, {
    day: "2-digit",
    month: "short",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  });
}

function pathSummary(path: RoutePath): string {
  return path.hops.map((hop) => hop.nodeId).join(" → ");
}

/**
 * Network Topology and Mesh Routing.
 *
 * Visualise → trace → review history. The graph, routes, spanning tree and
 * history all come from `/api/topology/*`; this page only chooses what to
 * emphasise. Routes are recomputed server-side as nodes drop off and come
 * back, so polling shows the mesh healing in place. Choices that should
 * survive navigation (view, traced node, history filters) live in the app
 * state store.
 */
function TopologyPage() {
  const toast = useToast();

  /* ---------- Operator choices: kept across navigation ---------- */
  const [view, setView] = usePersistentState<ViewMode>("topology.view", "mesh", { session: true });
  const [traceKey, setTraceKey] = usePersistentState<string>("topology.traceKey", "", { session: true });
  const [historyKey, setHistoryKey] = usePersistentState<string>("topology.historyKey", "", { session: true });
  const [hours, setHours] = usePersistentState<number>("topology.hours", 24, { session: true });
  const [sort, setSort] = usePersistentState<HistorySort>("topology.sort", "Newest", { session: true });
  const [readingType, setReadingType] = usePersistentState<ReadingType | "">("topology.readingType", "", { session: true });
  const [customFrom, setCustomFrom] = usePersistentState<string>("topology.customFrom", defaultCustomRange().from, { session: true });
  const [customTo, setCustomTo] = usePersistentState<string>("topology.customTo", defaultCustomRange().to, { session: true });
  const [scanRoot, setScanRoot] = usePersistentState<string>("topology.scanRoot", "ALL", { session: true });
  const [scanZone, setScanZone] = usePersistentState<string>("topology.scanZone", "", { session: true });
  const [algorithm, setAlgorithm] = usePersistentState<TraversalAlgorithm>("topology.algorithm", "Bfs", { session: true });
  const [page, setPage] = useState(1);

  const [filter, setFilter] = useState("");
  const [zoom, setZoom] = useState(MIN_ZOOM);
  const [macInput, setMacInput] = useState(traceKey);

  /* ---------- Data ---------- */
  const [topology, setTopology] = usePersistentState<MeshTopology | null>("topology.data", null);
  const [trace, setTrace] = useState<RouteTrace | null>(null);
  const [history, setHistory] = useState<TelemetryHistory | null>(null);
  const [scan, setScan] = useState<ConnectivityScan | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [historyError, setHistoryError] = useState<string | null>(null);
  const [scanError, setScanError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  /* ---------- Polling: topology and the traced route together ---------- */
  usePolling(
    async (signal) => {
      try {
        const [mesh, route] = await Promise.all([
          getTopology(signal),
          traceKey ? traceRoute(traceKey, signal) : Promise.resolve(null),
        ]);
        setTopology(mesh);
        setTrace(route);
        setError(null);
      } catch (err) {
        if (!isAbortError(err)) setError(describeError(err, "Could not load the mesh."));
      }
    },
    { intervalMs: REFRESH_MS, key: traceKey }
  );

  const sensors = (topology?.nodes ?? []).filter((node) => node.kind === "Sensor");
  const activeHistoryKey = historyKey || sensors[0]?.id || "";

  const isCustom = hours === CUSTOM_RANGE;
  const customFromDate = new Date(customFrom);
  const customToDate = new Date(customTo);
  const customRangeError = !isCustom
    ? null
    : Number.isNaN(customFromDate.getTime()) || Number.isNaN(customToDate.getTime())
      ? "Enter both a start and an end time."
      : customFromDate >= customToDate
        ? "The start of the range must be before the end."
        : null;

  usePolling(
    async (signal) => {
      if (!activeHistoryKey) return;
      if (customRangeError) {
        setHistoryError(customRangeError);
        return;
      }
      try {
        const result = await getTelemetryHistory(
          activeHistoryKey,
          {
            hours: isCustom ? undefined : hours,
            from: isCustom ? customFromDate.toISOString() : null,
            to: isCustom ? customToDate.toISOString() : null,
            sort,
            readingType: readingType || null,
            page,
            pageSize: HISTORY_PAGE_SIZE,
          },
          signal
        );
        setHistory(result);
        setHistoryError(null);
      } catch (err) {
        if (!isAbortError(err)) setHistoryError(describeError(err, "Could not load the history."));
      }
    },
    {
      intervalMs: HISTORY_REFRESH_MS,
      key: JSON.stringify([activeHistoryKey, hours, sort, readingType, page, isCustom ? customFrom : "", isCustom ? customTo : ""]),
    }
  );

  /* ---------- Polling: the BFS / DFS connectivity scan ---------- */
  usePolling(
    async (signal) => {
      try {
        setScan(await scanConnectivity(scanRoot, scanZone || null, algorithm, signal));
        setScanError(null);
      } catch (err) {
        if (!isAbortError(err)) setScanError(describeError(err, "Could not run the scan."));
      }
    },
    { intervalMs: REFRESH_MS, key: JSON.stringify([scanRoot, scanZone, algorithm]) }
  );

  /* ---------- Actions ---------- */

  /** Re-run the scan straight after a node goes down or comes back, rather than waiting for the next poll. */
  const refreshScan = async () => {
    try {
      setScan(await scanConnectivity(scanRoot, scanZone || null, algorithm));
    } catch {
      // The poll reports errors; a missed refresh here is picked up on the next tick.
    }
  };

  const selectNode = (nodeId: string) => {
    setMacInput(nodeId);
    setTraceKey(nodeId);
    setView("route");
    if (sensors.some((node) => node.id === nodeId)) {
      setHistoryKey(nodeId);
      setPage(1);
    }
  };

  const onTrace = (event: FormEvent) => {
    event.preventDefault();
    const key = macInput.trim();
    if (!key) return;
    setTraceKey(key);
    setView("route");
  };

  const onRecompute = async () => {
    setBusy(true);
    try {
      const mesh = await recomputeTopology();
      setTopology(mesh);
      toast.show({
        tone: "success",
        title: "Routes recomputed",
        message: `Dijkstra ${formatNumber(mesh.summary.dijkstraMicroseconds)} µs · Prim ${formatNumber(mesh.summary.primMicroseconds)} µs · Kruskal ${formatNumber(mesh.summary.kruskalMicroseconds)} µs over ${mesh.summary.activeLinks} active links.`,
      });
      if (traceKey) setTrace(await traceRoute(traceKey));
      void refreshScan();
    } catch (err) {
      const message = describeError(err, "Recompute failed.");
      if (message) toast.show({ tone: "error", title: "Recompute failed", message });
    } finally {
      setBusy(false);
    }
  };

  const onToggleFailure = async (nodeId: string, failed: boolean) => {
    setBusy(true);
    try {
      const result = await setNodeFailure(nodeId, failed);
      setTopology(result.topology);
      toast.show({
        tone: result.isolatedNodes.length > 0 && failed ? "warning" : "info",
        title: failed ? `${nodeId} taken offline` : `${nodeId} restored`,
        message: result.message,
      });
      if (traceKey) setTrace(await traceRoute(traceKey));
      void refreshScan();
    } catch (err) {
      const message = describeError(err, "The change was not applied.");
      if (message) toast.show({ tone: "error", title: "Could not change the node", message });
    } finally {
      setBusy(false);
    }
  };

  const onClearFailures = async () => {
    setBusy(true);
    try {
      setTopology(await clearNodeFailures());
      if (traceKey) setTrace(await traceRoute(traceKey));
      void refreshScan();
    } catch (err) {
      const message = describeError(err, "Could not clear the outages.");
      if (message) toast.show({ tone: "error", title: "Could not clear outages", message });
    } finally {
      setBusy(false);
    }
  };

  /* ---------- Derived view ---------- */

  const summary = topology?.summary;
  const traced = trace?.found ? trace.node : null;
  const primary = trace?.primary ?? null;
  const simulatedCount = topology?.nodes.filter((node) => node.isSimulatedFailure).length ?? 0;
  const gateways = topology?.nodes.filter((node) => node.kind === "Gateway") ?? [];
  const spanning = trace?.spanning ?? null;

  // What the graph emphasises depends on the view.
  const highlight =
    view === "route"
      ? { links: primary?.linkIds ?? [], nodes: primary?.hops.map((hop) => hop.nodeId) ?? [] }
      : view === "mst"
        ? { links: spanning?.linkIds ?? [], nodes: spanning?.hops.map((hop) => hop.nodeId) ?? [] }
        : view === "scan" && scan
          ? {
              links: scan.traversalLinkIds,
              // The subnet's devices, plus any relays outside it that the traversal passed through.
              nodes: [
                ...gateways.map((node) => node.id),
                ...scan.nodes.map((node) => node.nodeId),
                ...scan.traversalLinkIds.flatMap((id) => id.split("|")),
              ],
            }
          : { links: [], nodes: [] };

  const canvasNote =
    view === "mst"
      ? `Spanning tree: ${summary?.mstEdgeCount} links, ${summary?.mstTotalCost} expected transmissions (Prim ${formatNumber(summary?.primMicroseconds ?? 0)} µs` +
        `, Kruskal ${summary?.mstVerified ? "agrees ✓" : `differs (${summary?.kruskalTotalCost})`})` +
        (spanning ? ` · ${traced?.id}'s power-efficient path: ${pathSummary(spanning)}` : " · trace a node to see its power-efficient path")
      : view === "route"
        ? primary ? `Route ${pathSummary(primary)}` : "Trace a node to highlight its route"
        : view === "scan"
          ? scan ? `${scan.algorithm.toUpperCase()} traversal tree from ${scan.root === "ALL" ? "every gateway" : scan.root}${scan.zone ? ` · subnet ${scan.zone}` : ""}` : "Running the scan…"
          : `Routing tree highlighted · Dijkstra ${formatNumber(summary?.dijkstraMicroseconds ?? 0)} µs · click a node to trace it`;

  return (
    <div className="topology-page">
      <header className="page-head">
        <div>
          <h1 className="page-title">Network Topology and Mesh Routing</h1>
          <p className="page-sub">
            Visualise the active IoT mesh, trace how any node&apos;s packets hop back to the
            gateway, and review a sensor&apos;s telemetry history.
          </p>
        </div>
        <div className="page-head-actions">
          <span className="page-refresh">
            {topology ? `Updated ${formatTime(topology.generatedUtc)}` : "Connecting…"}
          </span>
          {simulatedCount > 0 && (
            <button type="button" className="filter-chip" onClick={onClearFailures} disabled={busy}>
              Clear {simulatedCount} outage{simulatedCount === 1 ? "" : "s"}
            </button>
          )}
          <button type="button" className="register-btn" onClick={onRecompute} disabled={busy || !topology}>
            Recompute routes
          </button>
        </div>
      </header>

      {error && <p className="panel-empty" role="alert">{error}</p>}

      {/* Mesh overview */}
      <section className="stat-row" aria-label="Topology overview">
        <StatTile
          label="Active nodes"
          value={summary ? `${summary.activeNodes} / ${summary.sensorNodes}` : "—"}
          hint={summary ? `${summary.offlineNodes} offline · ${summary.degradedNodes} degraded · ${summary.isolatedNodes} isolated` : "Online of total registered"}
          tone={summary && summary.isolatedNodes > 0 ? "danger" : summary && summary.offlineNodes > 0 ? "warning" : "default"}
          progress={summary && summary.sensorNodes > 0 ? (summary.activeNodes / summary.sensorNodes) * 100 : undefined}
        />
        <StatTile
          label="Mesh links"
          value={summary ? `${summary.activeLinks} / ${summary.totalLinks}` : "—"}
          hint={summary ? `${summary.redundantLinks} spare paths for self-healing` : "Edges in the network graph"}
        />
        <StatTile
          label="Gateways"
          value={summary ? `${summary.onlineGateways} / ${summary.gateways}` : "—"}
          hint="Root nodes for routing"
          tone={summary && summary.onlineGateways < summary.gateways ? "danger" : "default"}
        />
        <StatTile
          label="Average hops"
          value={summary ? formatNumber(summary.averageHops, 2) : "—"}
          hint={summary ? `Node → gateway · max ${summary.maxHops} · ${summary.reroutedNodes} re-routed` : "Node → gateway"}
          tone={summary && summary.reroutedNodes > 0 ? "accent" : "default"}
        />
        <StatTile
          label="MST total cost"
          value={summary ? formatNumber(summary.mstTotalCost, 2) : "—"}
          hint={
            summary
              ? `${summary.mstEdgeCount} links vs ${formatNumber(summary.activeMeshCost, 1)} for the full mesh · Kruskal ${summary.mstVerified ? "agrees ✓" : "differs"}`
              : "Sum of spanning-tree weights"
          }
          tone={summary && !summary.mstVerified ? "danger" : "default"}
        />
      </section>

      <div className="topology-grid">
        {/* 1. Topology dashboard */}
        <section className="topology-panel" aria-label="Mesh network graph">
          <header className="panel-head">
            <h2>Active mesh network</h2>
            <div className="filter-chips" role="group" aria-label="Graph view">
              {VIEW_MODES.map((mode) => (
                <button
                  key={mode.id}
                  type="button"
                  className={`filter-chip${view === mode.id ? " active" : ""}`}
                  aria-pressed={view === mode.id}
                  onClick={() => setView(mode.id)}
                >
                  {mode.label}
                </button>
              ))}
            </div>
          </header>

          <div className="topology-toolbar">
            <input
              type="search"
              className="topology-input"
              placeholder="Filter by zone, node or MAC…"
              aria-label="Filter graph"
              value={filter}
              onChange={(event) => setFilter(event.target.value)}
              list="topology-zones"
            />
            <datalist id="topology-zones">
              {topology?.zones.map((zone) => <option key={zone} value={zone} />)}
            </datalist>
            <div className="topology-zoom" role="group" aria-label="Zoom">
              <button type="button" className="topology-icon-btn" aria-label="Zoom out" disabled={zoom <= MIN_ZOOM} onClick={() => setZoom((value) => Math.max(MIN_ZOOM, value / 1.5))}>−</button>
              <button type="button" className="topology-icon-btn" aria-label="Reset view" disabled={zoom === MIN_ZOOM} onClick={() => setZoom(MIN_ZOOM)}>⟲</button>
              <button type="button" className="topology-icon-btn" aria-label="Zoom in" disabled={zoom >= MAX_ZOOM} onClick={() => setZoom((value) => Math.min(MAX_ZOOM, value * 1.5))}>+</button>
            </div>
          </div>

          <div className="topology-canvas">
            {topology ? (
              <MeshCanvas
                nodes={topology.nodes}
                links={topology.links}
                view={view}
                routeLinkIds={highlight.links}
                routeNodeIds={highlight.nodes}
                selectedId={traced?.id ?? null}
                filter={filter.trim().toLowerCase()}
                zoom={zoom}
                onSelect={selectNode}
              />
            ) : (
              <span className="topology-canvas-note">Loading the live topology…</span>
            )}
            {topology && <span className="topology-canvas-note">{canvasNote}</span>}
          </div>

          <ul className="topology-legend" aria-label="Legend">
            <li><span className="legend-dot legend-gateway" /> Gateway</li>
            <li><span className="legend-dot legend-sensor" /> Sensor node</li>
            <li><span className="legend-dot legend-degraded" /> Degraded</li>
            <li><span className="legend-dot legend-rerouted" /> Re-routed</li>
            <li><span className="legend-dot legend-offline" /> Offline / isolated</li>
            <li><span className="legend-line" /> Mesh link (weighted)</li>
            <li><span className="legend-line legend-line-tree" /> {view === "mst" ? "Spanning-tree link" : "Routing-tree link"}</li>
            <li>
              <span className="legend-line legend-line-route" />{" "}
              {view === "mst" ? "Power-efficient path (MST)" : view === "scan" ? "Traversal tree" : "Active route"}
            </li>
          </ul>
        </section>

        {/* 2. Route tracker */}
        <aside className="topology-panel route-panel" aria-label="Route tracker">
          <header className="panel-head">
            <h2>Route tracker</h2>
            <span className="panel-head-count">by MAC / node ID</span>
          </header>

          <form className="route-form" onSubmit={onTrace}>
            <label className="route-label" htmlFor="route-mac">Node MAC address or identifier</label>
            <input
              id="route-mac"
              className="topology-input"
              placeholder="e.g. AA:BB:CC:DD:EE:FF or ENV-001"
              value={macInput}
              onChange={(event) => setMacInput(event.target.value)}
              list="topology-node-ids"
            />
            <datalist id="topology-node-ids">
              {topology?.nodes.map((node) => <option key={node.id} value={node.id}>{node.macAddress}</option>)}
            </datalist>
            <button type="submit" className="register-btn" disabled={!macInput.trim()}>
              Trace route
            </button>
          </form>

          <dl className="route-facts">
            <div><dt>Hops</dt><dd>{primary ? primary.hopCount : "—"}</dd></div>
            <div><dt>Path cost</dt><dd>{primary ? formatNumber(primary.totalCost, 2) : "—"}</dd></div>
            <div><dt>Latency</dt><dd>{primary ? `${formatNumber(primary.totalLatencyMs, 1)} ms` : "—"}</dd></div>
          </dl>

          {primary ? (
            <ol className="route-hops" aria-label="Route hops">
              {primary.hops.map((hop) => (
                <li key={hop.nodeId} className={`route-hop${hop.kind === "Gateway" ? " route-hop-gateway" : ""}`}>
                  <span className="route-hop-index">{hop.index + 1}</span>
                  <div>
                    <span className="route-hop-name">
                      {hop.nodeId} <span className="route-hop-meta">{hop.kind === "Gateway" ? "gateway" : hop.index === 0 ? "source" : "relay"}</span>
                    </span>
                    <span className="route-hop-meta">
                      {hop.macAddress}
                      {hop.linkCost !== null && ` · link ${hop.linkCost} · ${hop.linkRssiDbm} dBm · ${hop.linkLatencyMs} ms`}
                    </span>
                  </div>
                </li>
              ))}
            </ol>
          ) : null}

          <p className={`panel-empty${trace && !trace.reachable && trace.found ? " route-warning" : ""}`}>
            {trace ? trace.message : "Enter a node identifier, or click a node on the graph, to see its path to the gateway."}
          </p>

          {trace?.found && (
            <dl className="route-alternatives">
              {trace.isRerouted && trace.designed && (
                <div><dt>Designed route</dt><dd>{pathSummary(trace.designed)} · cost {trace.designed.totalCost}</dd></div>
              )}
              {trace.backup && primary && primary.hopCount > 0 && (
                <div>
                  <dt>Backup (avoids {primary.hops[1]?.nodeId})</dt>
                  <dd>{pathSummary(trace.backup)} · cost {trace.backup.totalCost}</dd>
                </div>
              )}
              {trace.spanning && (
                <div>
                  <dt>Power-efficient path (MST)</dt>
                  <dd>
                    {pathSummary(trace.spanning)} · {trace.spanning.totalCost} expected transmissions
                    {primary && trace.spanning.totalCost !== primary.totalCost && ` vs ${primary.totalCost} on the shortest route`}
                  </dd>
                </div>
              )}
            </dl>
          )}

          {traced && (
            <div className="route-actions">
              {traced.isSimulatedFailure ? (
                <button type="button" className="filter-chip" disabled={busy} onClick={() => onToggleFailure(traced.id, false)}>
                  Restore {traced.id}
                </button>
              ) : (
                <button type="button" className="filter-chip" disabled={busy || traced.state === "Offline"} onClick={() => onToggleFailure(traced.id, true)}>
                  Simulate failure of {traced.id}
                </button>
              )}
            </div>
          )}

          {topology && topology.criticalRelays.length > 0 && (
            <div className="route-relays">
              <span className="route-label">Critical relays</span>
              <ul>
                {topology.criticalRelays.map((relay) => (
                  <li key={relay.nodeId}>
                    <button type="button" className="route-relay" onClick={() => selectNode(relay.nodeId)}>
                      <span className="mono">{relay.nodeId}</span>
                      <span>{relay.zone}</span>
                      <span>{relay.dependants} depend on it</span>
                    </button>
                  </li>
                ))}
              </ul>
            </div>
          )}
        </aside>
      </div>

      {/* 3. Connectivity scan: BFS / DFS */}
      <section className="topology-panel" aria-label="Connectivity scan">
        <header className="panel-head">
          <h2>
            Connectivity scan
            <span className="panel-head-count"> · who is connected to a gateway or subnet, who is down, and where to hop</span>
          </h2>
          <div className="history-controls">
            <select
              className="topology-input"
              aria-label="Start from gateway"
              value={scanRoot}
              onChange={(event) => setScanRoot(event.target.value)}
            >
              <option value="ALL">All gateways</option>
              {gateways.map((node) => (
                <option key={node.id} value={node.id}>{node.id} — {node.name}{node.state === "Offline" ? " (down)" : ""}</option>
              ))}
            </select>
            <select
              className="topology-input"
              aria-label="Subnet"
              value={scanZone}
              onChange={(event) => setScanZone(event.target.value)}
            >
              <option value="">Whole mesh</option>
              {topology?.zones.map((zone) => <option key={zone} value={zone}>Subnet: {zone}</option>)}
            </select>
            <div className="filter-chips" role="group" aria-label="Traversal algorithm">
              {ALGORITHMS.map((item) => (
                <button
                  key={item.id}
                  type="button"
                  className={`filter-chip${algorithm === item.id ? " active" : ""}`}
                  aria-pressed={algorithm === item.id}
                  onClick={() => setAlgorithm(item.id)}
                >
                  {item.label}
                </button>
              ))}
            </div>
            <button type="button" className="filter-chip" onClick={() => setView("scan")} disabled={view === "scan"}>
              Show on graph
            </button>
          </div>
        </header>

        {scanError && <p className="panel-empty" role="alert">{scanError}</p>}
        {scan && <p className={`panel-empty${scan.rootOnline ? "" : " route-warning"}`}>{scan.message}</p>}

        {scan && (
          <ul className="history-metrics" aria-label="Scan summary">
            <li>
              <span className="route-label">Connected</span>
              <span className="mono">{scan.connectedCount} / {scan.inScope}</span>
              <span className="route-hop-meta">max depth {scan.maxDepth}</span>
            </li>
            <li>
              <span className="route-label">Down</span>
              <span className="mono">{scan.downCount}</span>
              <span className="route-hop-meta">never entered by the traversal</span>
            </li>
            <li>
              <span className="route-label">Cut off</span>
              <span className="mono">{scan.cutOffCount}</span>
              <span className="route-hop-meta">live, but no path to a gateway</span>
            </li>
            {scan.root !== "ALL" && (
              <li>
                <span className="route-label">Other gateway</span>
                <span className="mono">{scan.otherGatewayCount}</span>
                <span className="route-hop-meta">reachable only via another root</span>
              </li>
            )}
            <li>
              <span className="route-label">{scan.algorithm.toUpperCase()}</span>
              <span className="mono">{formatNumber(scan.traversalMicroseconds)} µs</span>
              <span className="route-hop-meta">{scan.totalVisited} nodes visited · O(V + E)</span>
            </li>
          </ul>
        )}

        {scan && scan.hopAdvice.length > 0 && (
          <div className="route-relays">
            <span className="route-label">Where to hop</span>
            <ul>
              {scan.hopAdvice.map((advice) => (
                <li key={advice.nodeId}>
                  <button type="button" className="route-relay" onClick={() => selectNode(advice.nodeId)}>
                    <span className="mono">{advice.nodeId}</span>
                    <span>{advice.advice}</span>
                  </button>
                </li>
              ))}
            </ul>
          </div>
        )}

        <div className="history-scroll">
          <table className="data-table">
            <thead>
              <tr>
                <th>Visit #</th>
                <th>Node</th>
                <th>Subnet</th>
                <th>Status</th>
                <th>Depth</th>
                <th>Hop via</th>
                <th>Detail</th>
              </tr>
            </thead>
            <tbody>
              {scan?.nodes.map((node) => (
                <tr key={node.nodeId} className="scan-row" onClick={() => selectNode(node.nodeId)}>
                  <td className="mono">{node.visitOrder ?? "—"}</td>
                  <td><span className="mono">{node.nodeId}</span> <span className="route-hop-meta">{node.name}</span></td>
                  <td>{node.zone}</td>
                  <td><span className={SCAN_BADGE[node.status].className}>{SCAN_BADGE[node.status].label}</span></td>
                  <td className="mono">{node.depth ?? "—"}</td>
                  <td className="mono">{node.hopVia ?? "—"}</td>
                  <td>{describeScanNode(node)}</td>
                </tr>
              ))}
              {scan && scan.nodes.length === 0 && (
                <tr className="history-placeholder-row"><td colSpan={7}>No devices in this subnet.</td></tr>
              )}
              {!scan && (
                <tr className="history-placeholder-row"><td colSpan={7}>Loading…</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </section>

      {/* 4. Telemetry history */}
      <section className="topology-panel" aria-label="Sensor telemetry history">
        <header className="panel-head">
          <h2>
            Telemetry history
            {history && <span className="panel-head-count"> · {history.sensorName} · {history.macAddress} · {history.zone}</span>}
            {history?.isCustomRange && (
              <span className="panel-head-count"> · {formatStamp(history.fromUtc)} – {formatStamp(history.toUtc)}</span>
            )}
          </h2>
          <div className="history-controls">
            <select
              className="topology-input"
              aria-label="Sensor"
              value={activeHistoryKey}
              onChange={(event) => { setHistoryKey(event.target.value); setReadingType(""); setPage(1); }}
              disabled={sensors.length === 0}
            >
              {sensors.length === 0 && <option value="">Select a sensor…</option>}
              {sensors.map((node) => (
                <option key={node.id} value={node.id}>{node.id} — {node.name}</option>
              ))}
            </select>
            <select
              className="topology-input"
              aria-label="Reading type"
              value={readingType}
              onChange={(event) => { setReadingType(event.target.value as ReadingType | ""); setPage(1); }}
            >
              <option value="">All metrics</option>
              {history?.availableReadingTypes.map((type) => <option key={type} value={type}>{type}</option>)}
            </select>
            <select
              className="topology-input"
              aria-label="Time range"
              value={hours}
              onChange={(event) => { setHours(Number(event.target.value)); setPage(1); }}
            >
              {TIME_RANGES.map((range) => <option key={range.hours} value={range.hours}>{range.label}</option>)}
            </select>
            {isCustom && (
              <>
                <input
                  type="datetime-local"
                  className="topology-input"
                  aria-label="From"
                  value={customFrom}
                  max={customTo}
                  onChange={(event) => { setCustomFrom(event.target.value); setPage(1); }}
                />
                <input
                  type="datetime-local"
                  className="topology-input"
                  aria-label="To"
                  value={customTo}
                  min={customFrom}
                  onChange={(event) => { setCustomTo(event.target.value); setPage(1); }}
                />
              </>
            )}
            <select
              className="topology-input"
              aria-label="Sort order"
              value={sort}
              onChange={(event) => { setSort(event.target.value as HistorySort); setPage(1); }}
            >
              <option value="Newest">Newest first</option>
              <option value="Oldest">Oldest first</option>
            </select>
          </div>
        </header>

        {historyError && <p className="panel-empty" role="alert">{historyError}</p>}

        {history && history.metrics.length > 0 && (
          <ul className="history-metrics" aria-label="Window summary">
            {history.metrics.map((metric) => (
              <li key={metric.readingType}>
                <span className="route-label">{humanise(metric.readingType)}</span>
                <span className="mono">
                  {formatNumber(metric.min, 2)}–{formatNumber(metric.max, 2)} {metric.unit} · avg {formatNumber(metric.average, 2)}
                </span>
                <span className="route-hop-meta">{metric.count} readings · {metric.breaches} breaches</span>
              </li>
            ))}
            {history.routeChanges.length > 0 && (
              <li>
                <span className="route-label">Route changes</span>
                <span className="mono">{history.routeChanges.length}</span>
                <span className="route-hop-meta">
                  Latest {formatTime(history.routeChanges[history.routeChanges.length - 1].timestampUtc)}: {history.routeChanges[history.routeChanges.length - 1].description}
                </span>
              </li>
            )}
          </ul>
        )}

        <div className="history-scroll">
          <table className="data-table">
            <thead>
              <tr>
                <th>Timestamp</th>
                <th>Reading type</th>
                <th>Value</th>
                <th>Route (hops)</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {history?.items.map((item) => (
                <tr key={`${item.timestampUtc}-${item.readingType}`}>
                  <td className="mono">{formatStamp(item.timestampUtc)}</td>
                  <td>{humanise(item.readingType)}</td>
                  <td className="mono">{formatNumber(item.value, item.readingType === "Motion" ? 0 : 2)} {item.unit}</td>
                  <td>{item.routeHops !== null ? `${item.routeHops} via ${item.routeGatewayId}` : "No route"}</td>
                  <td>
                    <span className={STATUS_BADGE[item.status]}>{item.status}</span>
                    {item.source === "Live" && <span className="badge"> live</span>}
                  </td>
                </tr>
              ))}
              {history && history.items.length === 0 && (
                <tr className="history-placeholder-row">
                  <td colSpan={5}>No readings in this window.</td>
                </tr>
              )}
              {!history && (
                <tr className="history-placeholder-row">
                  <td colSpan={5}>Loading…</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        <footer className="history-foot">
          <span className="panel-head-count">
            {history
              ? `${formatNumber(history.totalCount)} entries · page ${history.page} of ${history.totalPages} · ` +
                `AVL range search touched ${formatNumber(history.nodesVisited)} of ${formatNumber(history.indexedReadings)} indexed readings ` +
                `(tree height ${history.treeHeight}) in ${formatNumber(history.windowReadMicroseconds)} µs`
              : "0 entries"}
          </span>
          <div className="topology-zoom">
            <button type="button" className="filter-chip" disabled={!history || history.page <= 1} onClick={() => setPage((value) => Math.max(1, value - 1))}>← Prev</button>
            <button type="button" className="filter-chip" disabled={!history || history.page >= history.totalPages} onClick={() => setPage((value) => value + 1)}>Next →</button>
          </div>
        </footer>
      </section>
    </div>
  );
}

export default TopologyPage;
