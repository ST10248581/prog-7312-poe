using SmartX.Api.Models;
using SmartX.Api.Models.Topology;

namespace SmartX.Api.Logic;

/// <summary>
/// Everything behind the Network Topology and Mesh Routing page: the mesh
/// graph, shortest-path routing to the gateways, the minimum spanning tree,
/// route tracing by MAC address or node id, simulated node failures and each
/// sensor's telemetry history.
/// </summary>
public interface ISmartXTopologyEngine
{
    /* ---------- Topology dashboard ---------- */

    /// <summary>The live mesh. Routes are recomputed first if any node has gone up or down since the last pass.</summary>
    MeshTopology GetTopology();

    /// <summary>
    /// Rebuilds the graph from the registry (picking up newly registered sensors)
    /// and recomputes every route and the spanning tree, whether or not anything changed.
    /// </summary>
    MeshTopology Recompute();

    /* ---------- Route tracker ---------- */

    /// <summary>Traces a node's route to its gateway, looked up by node id or MAC address in any notation.</summary>
    RouteTrace TraceRoute(string key);

    /// <summary>
    /// Takes a node off the mesh (or brings it back), recomputes, and reports
    /// which nodes healed onto another path and which were cut off. Null when
    /// the key matches no node.
    /// </summary>
    NodeFailureResult? SetSimulatedFailure(string key, bool failed);

    /// <summary>Brings every simulated failure back online.</summary>
    MeshTopology ClearSimulatedFailures();

    /* ---------- Connectivity scan ---------- */

    /// <summary>
    /// BFS or DFS from a gateway (or "ALL") over the live mesh: which devices in
    /// the subnet are connected, which are down or cut off, and where nodes whose
    /// relay is down should hop. Null when <paramref name="root"/> is not a gateway.
    /// </summary>
    ConnectivityScan? ScanConnectivity(string root, string? zone, TraversalAlgorithm algorithm);

    /* ---------- Telemetry history ---------- */

    /// <summary>
    /// A sensor's readings over the window, cut out of its AVL trees, merged,
    /// ordered and paged. With <paramref name="fromUtc"/> and <paramref name="toUtc"/>
    /// the window is that exact range (e.g. 14:00–16:00); otherwise it is the last
    /// <paramref name="hours"/>. Null when the key matches no sensor.
    /// </summary>
    TelemetryHistory? GetTelemetryHistory(string key, int hours, HistorySort sort, ReadingType? readingType, int page, int pageSize,
        DateTime? fromUtc = null, DateTime? toUtc = null);

    /* ---------- Live loop ---------- */

    /// <summary>Recomputes routes if the set of down nodes has changed. Called on a timer by <see cref="TopologyMonitor"/>.</summary>
    void Refresh();
}
