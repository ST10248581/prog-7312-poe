// =============================================================================
// CODE ATTRIBUTION — Part 3 Technical Requirements (Network Topology and Mesh Routing)
//
// This class is where the Part 3 data structures — graphs, heaps and trees —
// and the routing algorithms do their work. They were written with reference
// to the sources below. The individual sections are marked with the matching
// [n] numbers, and the full reference list is repeated in README.md.
//
// Code Attribution [26]
// Author: Cormen, T.H., Leiserson, C.E., Rivest, R.L. and Stein, C.
// Year: 2022
// Title: Introduction to Algorithms, Chapter 6: Heapsort (Heaps and priority queues)
// Version: 4th edition
// Type: [Source code]
// Available at: <https://mitpress.mit.edu/9780262046305/introduction-to-algorithms/>
// Accessed: [Accessed 2 November 2026]
// Modifications: The hand-written BinaryHeap<TElement,TPriority> is the
//   frontier of both the Dijkstra search and the Prim build below.
// Reference: Cormen, T.H., Leiserson, C.E., Rivest, R.L. and Stein, C., 2022. Introduction to Algorithms, Chapter 6: Heapsort (4th edition) [Source code] Available at: <https://mitpress.mit.edu/9780262046305/introduction-to-algorithms/> [Accessed 2 November 2026].
//
// Code Attribution [27]
// Author: Sedgewick, R. and Wayne, K.
// Year: 2011
// Title: Algorithms, Section 4.3: Minimum Spanning Trees (EdgeWeightedGraph)
// Version: 4th edition
// Type: [Source code]
// Available at: <https://algs4.cs.princeton.edu/43mst/>
// Accessed: [Accessed 2 November 2026]
// Modifications: The mesh is held in MeshGraph<TNode,TEdge>, an adjacency-list
//   adaptation of EdgeWeightedGraph. BuildGraph links every node to its nearest
//   neighbours in radio range and stitches any cluster cut off from the
//   gateways back on with its shortest bridging link.
// Reference: Sedgewick, R. and Wayne, K., 2011. Algorithms, Section 4.3: Minimum Spanning Trees (4th edition) [Source code] Available at: <https://algs4.cs.princeton.edu/43mst/> [Accessed 2 November 2026].
//
// Code Attribution [28]
// Author: Dijkstra, E.W.
// Year: 1959
// Title: A note on two problems in connexion with graphs
// Type: [Source code]
// Available at: <https://doi.org/10.1007/BF01386390>
// Accessed: [Accessed 2 November 2026]
// Modifications: Implemented the shortest-path algorithm with a binary heap
//   and lazy deletion, seeded from every live gateway at once (multi-source),
//   so each node finds its cheapest path to whichever gateway is nearest.
//   Offline nodes are never relaxed, which is what makes the mesh heal around
//   them, and a degraded relay adds a penalty to paths through it.
// Reference: Dijkstra, E.W., 1959. A note on two problems in connexion with graphs [Source code] Available at: <https://doi.org/10.1007/BF01386390> [Accessed 2 November 2026].
//
// Code Attribution [29]
// Author: Prim, R.C.
// Year: 1957
// Title: Shortest connection networks and some generalizations
// Type: [Source code]
// Available at: <https://doi.org/10.1002/j.1538-7305.1957.tb01515.x>
// Accessed: [Accessed 2 November 2026]
// Modifications: Implemented Prim's algorithm with a binary heap of candidate
//   edges, grown from all live gateways together — equivalent to joining the
//   gateways with zero-cost edges — so the result is a minimum spanning forest
//   with one tree per gateway over the live nodes only.
// Reference: Prim, R.C., 1957. Shortest connection networks and some generalizations [Source code] Available at: <https://doi.org/10.1002/j.1538-7305.1957.tb01515.x> [Accessed 2 November 2026].
//
// Code Attribution [30]
// Author: De Couto, D.S.J., Aguayo, D., Bicket, J. and Morris, R.
// Year: 2003
// Title: A high-throughput path metric for multi-hop wireless routing
// Type: [Source code]
// Available at: <https://doi.org/10.1145/938985.939000>
// Accessed: [Accessed 2 November 2026]
// Modifications: Used the ETX metric (expected transmissions = 1 / delivery
//   ratio) as the link weight. The delivery ratio is derived from a simulated
//   RSSI rather than measured with probe packets.
// Reference: De Couto, D.S.J., Aguayo, D., Bicket, J. and Morris, R., 2003. A high-throughput path metric for multi-hop wireless routing [Source code] Available at: <https://doi.org/10.1145/938985.939000> [Accessed 2 November 2026].
//
// Code Attribution [31]
// Author: Microsoft
// Year: 2025
// Title: SortedSet<T> Class (System.Collections.Generic)
// Type: [Source code]
// Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.sortedset-1>
// Accessed: [Accessed 2 November 2026]
// Modifications: No code copied. The hand-written AvlTree's Range query
//   mirrors SortedSet.GetViewBetween, and a duplicate reading (same time and
//   metric) is refused by Add exactly as SortedSet.Add refuses one.
// Reference: Microsoft, 2025. SortedSet<T> Class (System.Collections.Generic) [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.sortedset-1> [Accessed 2 November 2026].
//
// Code Attribution [33]
// Author: Adelson-Velsky, G.M. and Landis, E.M.
// Year: 1962
// Title: An algorithm for the organization of information
// Type: [Source code]
// Available at: <https://zhjwpku.com/assets/pdf/AED2-10-avl-paper.pdf>
// Accessed: [Accessed 6 November 2026]
// Modifications: Each sensor's stored history is indexed in one hand-written
//   AVL tree per metric, keyed by timestamp. Readings arrive in time order —
//   the input that degrades a plain BST into a list — and the rotations keep
//   each tree O(log n) tall, so "moisture on ENV-001 between 14:00 and 16:00"
//   reads only that metric's tree, and only the part of it inside the range.
// Reference: Adelson-Velsky, G.M. and Landis, E.M., 1962. An algorithm for the organization of information [Source code] Available at: <https://zhjwpku.com/assets/pdf/AED2-10-avl-paper.pdf> [Accessed 6 November 2026].
//
// Code Attribution [34]
// Author: Sedgewick, R. and Wayne, K.
// Year: 2011
// Title: Algorithms, Section 3.2: Binary Search Trees (ordered range search)
// Version: 4th edition
// Type: [Source code]
// Available at: <https://algs4.cs.princeton.edu/32bst/>
// Accessed: [Accessed 6 November 2026]
// Modifications: The pruned range search (enter a subtree only if it can hold
//   keys in [lo, hi]) answers each history window; the per-metric results are
//   then k-way merged through the binary heap [26].
// Reference: Sedgewick, R. and Wayne, K., 2011. Algorithms, Section 3.2: Binary Search Trees (4th edition) [Source code] Available at: <https://algs4.cs.princeton.edu/32bst/> [Accessed 6 November 2026].
//
// Code Attribution [35]
// Author: Kruskal, J.B.
// Year: 1956
// Title: On the shortest spanning subtree of a graph and the traveling salesman problem
// Type: [Source code]
// Available at: <https://doi.org/10.1090/S0002-9939-1956-0078686-7>
// Accessed: [Accessed 6 November 2026]
// Modifications: Kruskal's algorithm over a hand-written DisjointSet runs
//   beside Prim on every recompute, with the live gateways pre-joined into one
//   root, as an independent check that the spanning tree is minimal.
// Reference: Kruskal, J.B., 1956. On the shortest spanning subtree of a graph and the traveling salesman problem [Source code] Available at: <https://doi.org/10.1090/S0002-9939-1956-0078686-7> [Accessed 6 November 2026].
//
// Code Attribution [36]
// Author: Sedgewick, R. and Wayne, K.
// Year: 2011
// Title: Algorithms, Section 4.1: Undirected Graphs (BreadthFirstPaths, DepthFirstPaths)
// Version: 4th edition
// Type: [Source code]
// Available at: <https://algs4.cs.princeton.edu/41graph/>
// Accessed: [Accessed 6 November 2026]
// Modifications: ScanConnectivity runs BFS or DFS from a gateway over live
//   nodes only, to list every device connected to it within a subnet (zone),
//   the ones that are down or cut off, and — from each visit's parent — the
//   relay a node should hop to when its designed relay is down.
// Reference: Sedgewick, R. and Wayne, K., 2011. Algorithms, Section 4.1: Undirected Graphs (4th edition) [Source code] Available at: <https://algs4.cs.princeton.edu/41graph/> [Accessed 6 November 2026].
//
// Code Attribution [32]
// Author: Winter, T. et al.
// Year: 2012
// Title: RPL: IPv6 Routing Protocol for Low-Power and Lossy Networks (RFC 6550)
// Type: [Source code]
// Available at: <https://www.rfc-editor.org/rfc/rfc6550>
// Accessed: [Accessed 2 November 2026]
// Modifications: No code copied. The shortest-path parents are assembled into
//   a routing tree per gateway, as RPL's DODAG is, and walked recursively to
//   count each relay's dependants.
// Reference: Winter, T. et al., 2012. RPL: IPv6 Routing Protocol for Low-Power and Lossy Networks (RFC 6550) [Source code] Available at: <https://www.rfc-editor.org/rfc/rfc6550> [Accessed 2 November 2026].
//
// Also used, as in Part 2: Dictionary<TKey,TValue> [17] for the node-id and
// MAC indexes, SortedList<TKey,TValue> [18] for each node's route history and
// HashSet<T> [19] for the simulated-failure set.
// =============================================================================

using System.Diagnostics;
using SmartX.Api.Data;
using SmartX.Api.Data.Collections;
using SmartX.Api.Data.Seeding;
using SmartX.Api.Models;
using SmartX.Api.Models.Stream;
using SmartX.Api.Models.Topology;
using SmartX.Api.Models.Validation;

namespace SmartX.Api.Logic;

/// <summary>
/// Central application-logic service for the Network Topology and Mesh Routing
/// page — the Part 3 counterpart of <see cref="SmartXTelemetryEngine"/> and
/// <see cref="SmartXCommandEngine"/>.
/// <para>
/// It models the deployment as a mesh: every sensor can talk to its nearest
/// neighbours in radio range, and packets hop from node to node until they
/// reach a gateway. Which nodes are up comes live from the command engine's
/// disconnected-node set, so when a device drops off the mesh on page 2 its
/// neighbours re-route around it here — the mesh "heals".
/// </para>
/// <para>
/// Each structure is chosen for the question it has to answer:
/// <list type="bullet">
///   <item><description><b>Graph</b> (adjacency list) — the mesh itself; a node's neighbours are reached in O(degree).</description></item>
///   <item><description><b>Binary heap</b> — the frontier of Dijkstra and Prim, so the cheapest candidate is always O(log n) away.</description></item>
///   <item><description><b>Dijkstra</b> — the fastest route from every node to its nearest live gateway.</description></item>
///   <item><description><b>Minimum spanning tree</b> (Prim, cross-checked by Kruskal) — the cheapest set of links that keeps every live node connected: the power-efficient backbone.</description></item>
///   <item><description><b>BFS / DFS</b> — which devices are connected to a gateway or subnet, which are down, and where to hop around them.</description></item>
///   <item><description><b>Routing tree</b> — the shortest-path parents as a tree per gateway, walked recursively to find the relays most nodes depend on.</description></item>
///   <item><description><b>AVL tree</b> (hand-written, one per sensor per metric) — the history, so a time range is cut out in O(log n + k).</description></item>
/// </list>
/// </para>
/// Registered as a singleton: the graph, the route history and the simulated
/// failures are live state shared between requests and <see cref="TopologyMonitor"/>.
/// </summary>
public class SmartXTopologyEngine : ISmartXTopologyEngine
{
    /* ---------- Layout ---------- */

    /// <summary>Zone clusters sit on a ring around the gateways.</summary>
    private const double ZoneRingRadius = 27;
    private const double ZoneClusterRadius = 12;

    /// <summary>Nodes closer than this are pushed apart, so the graph stays legible.</summary>
    private const double MinNodeSpacing = 4.5;
    private const int LayoutIterations = 40;
    private const double LayoutMargin = 4;

    /* ---------- Radio model ---------- */

    /// <summary>Metres per layout unit: a 0–100 canvas is a 150 m site.</summary>
    private const double MetresPerUnit = 1.5;

    /// <summary>Sensor-to-sensor radio range, in layout units (31.5 m).</summary>
    private const double RadioRange = 21;

    /// <summary>Gateways have better antennas, so they hear further (37.5 m).</summary>
    private const double GatewayRange = 25;

    /// <summary>Links a sensor opens to its nearest neighbours. Links are symmetric, so degree can be higher.</summary>
    private const int MaxNeighbours = 5;

    private const int MaxGatewayLinks = 9;

    /// <summary>Extra cost of relaying through a sensor in a warning state, so routes prefer healthy relays.</summary>
    private const double DegradedRelayPenalty = 0.75;

    /* ---------- Trees ---------- */

    /// <summary>Guard on the recursive tree walks. Far deeper than any real route.</summary>
    private const int MaxTreeDepth = 64;

    private const int CriticalRelayCount = 5;

    /* ---------- History ---------- */

    private const int MaxHistoryHours = 168;

    /// <summary>Longest explicit from/to range accepted: 31 days.</summary>
    private const int MaxCustomRangeHours = 744;

    /// <summary>The key the scan endpoint uses for "start from every live gateway".</summary>
    public const string AllGateways = "ALL";
    private const int MaxRouteSnapshotsPerNode = 500;
    private const int MaxPageSize = 200;

    private const double Epsilon = 1e-9;

    private static readonly (string Id, string Name, string Mac, double X, double Y)[] Gateways =
    [
        ("GW-01", "West Gateway", "02:53:58:47:57:01", 40, 50),
        ("GW-02", "East Gateway", "02:53:58:47:57:02", 60, 50)
    ];

    private readonly ISmartXDataStore _store;
    private readonly SeedOptions _options;
    private readonly ISmartXCommandEngine _commands;

    /// <summary>One lock over all engine state.</summary>
    private readonly object _sync = new();

    /* ---------- Graph [27] and indexes [17] ---------- */

    private readonly MeshGraph<NodeInfo, LinkInfo> _graph = new();
    private readonly Dictionary<string, NodeInfo> _nodesByMac = new(StringComparer.OrdinalIgnoreCase);
    private int _registeredProfileCount = -1;

    /* ---------- Live state ---------- */

    /// <summary>Nodes taken down from this page [19]. A HashSet, so each probe during routing is O(1).</summary>
    private readonly HashSet<string> _simulatedFailures = new(StringComparer.OrdinalIgnoreCase);

    private Dictionary<string, OfflineReason> _down = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _degraded = new(StringComparer.OrdinalIgnoreCase);
    private string? _healthSignature;

    private RoutingState _current = RoutingState.Empty;

    /// <summary>Routes with every node up — the "designed" paths a reroute is measured against.</summary>
    private RoutingState _designed = RoutingState.Empty;

    private SpanningForest _mst = SpanningForest.Empty;

    private int _routeVersion;
    private DateTime _lastRecomputedUtc;
    private double _dijkstraMicroseconds;
    private double _primMicroseconds;
    private (int Edges, double Cost, double Microseconds) _kruskal;

    /* ---------- Route history — SortedList [18] ---------- */

    /// <summary>
    /// Each node's route over time, keyed by when it took effect. A reading's
    /// route is the last snapshot at or before its timestamp, found by binary search.
    /// </summary>
    private readonly Dictionary<string, SortedList<DateTime, RouteSnapshot>> _routeHistory = new(StringComparer.OrdinalIgnoreCase);

    /* ---------- History index — AVL trees [33] ---------- */

    /// <summary>
    /// Each sensor's stored readings, one AVL tree per metric keyed by
    /// timestamp. Readings page 1 ingests later are inserted incrementally.
    /// </summary>
    private readonly Dictionary<Guid, SensorArchive> _archive = new();

    public SmartXTopologyEngine(ISmartXDataStore store, SeedOptions options, ISmartXCommandEngine commands)
    {
        _store = store;
        _options = options;
        _commands = commands;
    }

    // =====================================================================
    // Topology dashboard
    // =====================================================================

    public MeshTopology GetTopology()
    {
        lock (_sync)
        {
            EnsureReady();
            RecomputeIfChanged(force: false);
            return BuildTopology();
        }
    }

    public MeshTopology Recompute()
    {
        lock (_sync)
        {
            // Rebuild from the registry too, so edits on page 1 (a new sensor,
            // a changed node id) are picked up without a restart.
            BuildGraph();
            RecomputeIfChanged(force: true);
            return BuildTopology();
        }
    }

    public void Refresh()
    {
        lock (_sync)
        {
            EnsureReady();
            RecomputeIfChanged(force: false);
        }
    }

    // =====================================================================
    // Route tracker
    // =====================================================================

    public RouteTrace TraceRoute(string key)
    {
        lock (_sync)
        {
            EnsureReady();
            RecomputeIfChanged(force: false);

            var trace = new RouteTrace { Key = key?.Trim() ?? string.Empty, GeneratedUtc = DateTime.UtcNow };
            var (node, matchedBy, normalised) = Resolve(trace.Key);
            trace.NormalisedKey = normalised;

            if (node is null)
            {
                trace.Message = $"No node or gateway matches \"{trace.Key}\".";
                return trace;
            }

            trace.Found = true;
            trace.MatchedBy = matchedBy;
            trace.Node = ToMeshNode(node);
            trace.Designed = BuildPath(node.Id, _designed);
            trace.Spanning = BuildSpanningPath(node.Id);

            if (_down.TryGetValue(node.Id, out var reason))
            {
                trace.Message = $"{node.Id} is offline ({Describe(reason)}). Nodes that relayed through it have been re-routed.";
                return trace;
            }

            var primary = BuildPath(node.Id, _current);
            if (primary is null)
            {
                trace.Message = $"{node.Id} is up but isolated: every path to a gateway runs through an offline node.";
                return trace;
            }

            trace.Reachable = true;
            trace.Primary = primary;
            trace.IsRerouted = IsRerouted(node.Id);

            if (node.Kind == MeshNodeKind.Gateway)
            {
                trace.Message = $"{node.Id} is a gateway: routes end here.";
                return trace;
            }

            // Backup: the best path if the first relay failed next. A node that
            // talks straight to a gateway has no relay to lose, so its backup
            // avoids that gateway instead.
            var firstRelay = primary.Hops[1].NodeId;
            var started = Stopwatch.GetTimestamp();
            var withoutRelay = RunDijkstra(id => IsUp(id) && !string.Equals(id, firstRelay, StringComparison.OrdinalIgnoreCase), _degraded.Contains);
            trace.BackupSearchMicroseconds = Math.Round(Stopwatch.GetElapsedTime(started).TotalMicroseconds, 2);
            trace.Backup = BuildPath(node.Id, withoutRelay);

            var designedRelay = trace.Designed?.Hops.ElementAtOrDefault(1)?.NodeId;
            trace.Message = !trace.IsRerouted
                ? $"{node.Id} reaches {primary.GatewayId} in {primary.HopCount} hop(s) on its designed route."
                : trace.Designed is not null && trace.Designed.HopCount != primary.HopCount
                    ? $"Self-healed: {node.Id} now reaches {primary.GatewayId} in {primary.HopCount} hop(s) instead of its designed {trace.Designed.HopCount}."
                    : $"Self-healed: {node.Id} now forwards via {firstRelay} instead of {designedRelay}, still {primary.HopCount} hop(s) to {primary.GatewayId}.";

            if (trace.Backup is null)
            {
                trace.Message += $" No backup route: losing {firstRelay} would cut it off.";
            }

            return trace;
        }
    }

    public NodeFailureResult? SetSimulatedFailure(string key, bool failed)
    {
        lock (_sync)
        {
            EnsureReady();
            RecomputeIfChanged(force: false);

            var (node, _, _) = Resolve(key?.Trim() ?? string.Empty);
            if (node is null)
            {
                return null;
            }

            var before = _current;

            // The node's subtree in the routing tree is exactly the set of nodes
            // whose traffic passes through it — the ones a failure will force to
            // find another way.
            var affected = before.TreeIndex.TryGetValue(node.Id, out var treeNode)
                ? CollectSubtree(treeNode, 0)
                : new List<string>();

            var changedSet = failed ? _simulatedFailures.Add(node.Id) : _simulatedFailures.Remove(node.Id);
            if (changedSet)
            {
                RecomputeIfChanged(force: true);
            }

            var after = _current;
            var changed = _graph.Nodes
                .Where(info => info.Kind == MeshNodeKind.Sensor && !string.Equals(info.Id, node.Id, StringComparison.OrdinalIgnoreCase))
                .Where(info => !SamePath(PathOf(info.Id, before), PathOf(info.Id, after)))
                .Select(info => info.Id)
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var isolated = _graph.Nodes
                .Where(info => info.Kind == MeshNodeKind.Sensor && IsUp(info.Id) && !after.Cost.ContainsKey(info.Id))
                .Select(info => info.Id)
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var result = new NodeFailureResult
            {
                NodeId = node.Id,
                IsSimulatedFailure = _simulatedFailures.Contains(node.Id),
                ChangedRoutes = changed,
                IsolatedNodes = isolated
            };

            if (failed)
            {
                result.AffectedNodes = affected.Count;
                result.HealedNodes = affected.Count(id => after.Cost.ContainsKey(id));
                result.Message = !changedSet
                    ? $"{node.Id} was already down."
                    : affected.Count == 0
                        ? $"{node.Id} is down. No other node relayed through it, so no route changed."
                        : $"{node.Id} is down. {result.HealedNodes} of {affected.Count} dependent node(s) healed onto another path" +
                          (affected.Count > result.HealedNodes ? $"; {affected.Count - result.HealedNodes} are cut off." : ".");
            }
            else
            {
                result.AffectedNodes = changed.Count;
                result.HealedNodes = changed.Count;
                result.Message = !changedSet
                    ? $"{node.Id} was not under a simulated outage."
                    : _down.TryGetValue(node.Id, out var stillDown)
                        ? $"Simulated outage cleared, but {node.Id} is still offline ({Describe(stillDown)})."
                        : $"{node.Id} is back on the mesh. {changed.Count} node(s) moved onto a better route.";
            }

            result.Topology = BuildTopology();
            return result;
        }
    }

    public MeshTopology ClearSimulatedFailures()
    {
        lock (_sync)
        {
            EnsureReady();
            if (_simulatedFailures.Count > 0)
            {
                _simulatedFailures.Clear();
                RecomputeIfChanged(force: true);
            }

            return BuildTopology();
        }
    }

    // =====================================================================
    // Telemetry history
    // =====================================================================

    /// <summary>
    /// The sensor's readings over the window: the stored history (cut out of its
    /// AVL trees) and the live intake's log, merged without duplicates, in time
    /// order, paged, with the route each reading took.
    /// </summary>
    public TelemetryHistory? GetTelemetryHistory(string key, int hours, HistorySort sort, ReadingType? readingType, int page, int pageSize,
        DateTime? fromUtc = null, DateTime? toUtc = null)
    {
        var size = Math.Clamp(pageSize, 5, MaxPageSize);
        var now = DateTime.UtcNow;
        var custom = fromUtc.HasValue && toUtc.HasValue;

        DateTime from, to, upperBound;
        int window;
        if (custom)
        {
            // An exact range, e.g. 14:00–16:00 today.
            from = AsUtc(fromUtc!.Value);
            to = AsUtc(toUtc!.Value);
            if (to < from) (from, to) = (to, from);
            if ((to - from).TotalHours > MaxCustomRangeHours) from = to.AddHours(-MaxCustomRangeHours);
            upperBound = to;
            window = Math.Max(1, (int)Math.Ceiling((to - from).TotalHours));
        }
        else
        {
            window = Math.Clamp(hours, 1, MaxHistoryHours);
            to = now;
            from = now.AddHours(-window);

            // A little slack for device clocks running ahead.
            upperBound = now.AddMinutes(5);
        }

        lock (_sync)
        {
            EnsureReady();
            RecomputeIfChanged(force: false);

            var (node, _, _) = Resolve(key?.Trim() ?? string.Empty);
            if (node?.Profile is not SensorProfile profile)
            {
                return null;
            }

            var started = Stopwatch.GetTimestamp();
            var limits = LimitsFor(profile);

            // ---- Archive: a range search on each metric's AVL tree [33], [34] ----
            // Filtering by metric picks the tree, so the other metrics are never read.
            var archive = ArchiveFor(profile, limits);
            var ranges = new List<List<TelemetryHistoryEntry>>();
            int nodesVisited = 0, indexed = 0, treeHeight = 0;

            foreach (var (type, tree) in archive.Trees.OrderBy(pair => pair.Key))
            {
                if (readingType is not null && type != readingType)
                {
                    continue;
                }

                ranges.Add(tree.Range(from, upperBound, out var visited));
                nodesVisited += visited;
                indexed += tree.Count;
                treeHeight = Math.Max(treeHeight, tree.Height);
            }

            var archived = MergeByTime(ranges);

            // ---- Live: the command engine's sorted log for the node [18] ----
            var live = _commands.GetSensorLog(node.Id, from)
                .Where(entry => entry.Kind == SensorLogKind.Reading && entry.ReadingType.HasValue && entry.Value.HasValue)
                .Where(entry => entry.TimestampUtc <= upperBound && (readingType is null || entry.ReadingType == readingType))
                .Select(entry => ToHistoryEntry(entry.TimestampUtc, entry.ReadingType!.Value, entry.Value!.Value, ReadingQuality.Good, false, HistorySource.Live, limits))
                .ToList();
            live.Sort(HistoryOrder.Instance);

            var merged = MergeSorted(archived, live).ToList();

            var totalPages = Math.Max(1, (int)Math.Ceiling(merged.Count / (double)size));
            var current = Math.Clamp(page, 1, totalPages);

            // Oldest-first is the merged order. Newest-first reads it backwards
            // by index — no reversed copy of the whole window.
            var items = new List<TelemetryHistoryEntry>(size);
            for (var offset = (current - 1) * size; offset < Math.Min(merged.Count, current * size); offset++)
            {
                var index = sort == HistorySort.Newest ? merged.Count - 1 - offset : offset;
                items.Add(Clone(merged[index]));
            }

            var elapsed = Stopwatch.GetElapsedTime(started);

            // Route in effect at each reading: floor search in the route history [18].
            foreach (var item in items)
            {
                var snapshot = SnapshotAt(node.Id, item.TimestampUtc);
                item.RouteHops = snapshot?.Hops;
                item.RouteGatewayId = snapshot?.GatewayId;
            }

            return new TelemetryHistory
            {
                NodeId = node.Id,
                SensorProfileId = profile.Id,
                SensorName = profile.Name,
                MacAddress = profile.MacAddress,
                Zone = profile.Zone,
                Hours = window,
                FromUtc = from,
                ToUtc = to,
                IsCustomRange = custom,
                Sort = sort,
                ReadingType = readingType,
                Page = current,
                PageSize = size,
                TotalCount = merged.Count,
                TotalPages = totalPages,
                Items = items,
                Metrics = Summarise(merged),
                RouteChanges = RouteChangesBetween(node.Id, from, to),
                AvailableReadingTypes = ReadingTypeProfile.ForCategory(profile.Category).ToList(),
                WindowReadMicroseconds = Math.Round(elapsed.TotalMicroseconds, 2),
                IndexedReadings = indexed,
                NodesVisited = nodesVisited,
                TreeHeight = treeHeight
            };
        }
    }

    /// <summary>Query-string dates may arrive local or unspecified; the history is stored in UTC.</summary>
    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    // =====================================================================
    // Connectivity scan — BFS / DFS [36]
    // =====================================================================

    /// <summary>
    /// Walks the live mesh from a gateway (or every gateway) with BFS or DFS.
    /// Offline nodes are never entered, so whatever the walk reaches is exactly
    /// what is still connected. Within the chosen subnet (zone) it then reports
    /// each device as connected, down, served by the other gateway or cut off,
    /// and for every node whose designed relay is down, the neighbour the walk
    /// reached it from — the relay to hop to instead.
    /// </summary>
    public ConnectivityScan? ScanConnectivity(string root, string? zone, TraversalAlgorithm algorithm)
    {
        lock (_sync)
        {
            EnsureReady();
            RecomputeIfChanged(force: false);

            var key = root?.Trim() ?? string.Empty;
            var all = string.Equals(key, AllGateways, StringComparison.OrdinalIgnoreCase);
            var gateways = _graph.Nodes.Where(node => node.Kind == MeshNodeKind.Gateway).Select(node => node.Id).ToList();

            string rootLabel;
            List<string> starts;
            if (all)
            {
                rootLabel = AllGateways;
                starts = gateways.Where(IsUp).ToList();
            }
            else
            {
                var (gateway, _, _) = Resolve(key);
                if (gateway is null || gateway.Kind != MeshNodeKind.Gateway)
                {
                    return null;
                }

                rootLabel = gateway.Id;
                starts = IsUp(gateway.Id) ? [gateway.Id] : [];
            }

            // Only live sensors can be entered: a gateway is a destination, never a relay.
            bool CanEnter(string id) => IsUp(id) && _graph[id].Kind == MeshNodeKind.Sensor;

            var started = Stopwatch.GetTimestamp();
            var visits = algorithm == TraversalAlgorithm.Dfs
                ? _graph.DepthFirst(starts, CanEnter)
                : _graph.BreadthFirst(starts, CanEnter);
            var elapsed = Stopwatch.GetElapsedTime(started);

            var reached = visits.ToDictionary(visit => visit.NodeId, StringComparer.OrdinalIgnoreCase);

            // A node the scanned gateway cannot reach may still reach the other
            // one; a BFS from every gateway tells "served elsewhere" from "cut off".
            var anyGateway = all
                ? reached
                : _graph.BreadthFirst(gateways.Where(IsUp), CanEnter).ToDictionary(visit => visit.NodeId, StringComparer.OrdinalIgnoreCase);

            var zoneFilter = string.IsNullOrWhiteSpace(zone) ? null : zone.Trim();
            var scope = _graph.Nodes
                .Where(node => node.Kind == MeshNodeKind.Sensor)
                .Where(node => zoneFilter is null || string.Equals(node.Zone, zoneFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var scan = new ConnectivityScan
            {
                Root = rootLabel,
                Algorithm = algorithm,
                Zone = zoneFilter,
                RootOnline = starts.Count > 0,
                InScope = scope.Count,
                TotalVisited = visits.Count,
                TraversalMicroseconds = Math.Round(elapsed.TotalMicroseconds, 2),
                GeneratedUtc = DateTime.UtcNow
            };

            var traversalLinks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var node in scope)
            {
                var item = new ScanNode
                {
                    NodeId = node.Id,
                    Name = node.Name,
                    Zone = node.Zone,
                    Kind = node.Kind,
                    OfflineReason = _down.GetValueOrDefault(node.Id, OfflineReason.None)
                };

                if (reached.TryGetValue(node.Id, out var visit))
                {
                    item.Status = ScanNodeStatus.Connected;
                    item.VisitOrder = visit.Order;
                    item.Depth = visit.Depth;
                    item.HopVia = visit.Parent;

                    // Walk the traversal tree back to the root, for the graph highlight.
                    for (var step = visit; step.ViaEdgeId is not null && traversalLinks.Add(step.ViaEdgeId); step = reached[step.Parent!])
                    {
                    }
                }
                else if (!IsUp(node.Id))
                {
                    item.Status = ScanNodeStatus.Down;
                    item.StrandedDependants = _designed.TreeIndex.TryGetValue(node.Id, out var designedNode) ? designedNode.SubtreeSize : 0;
                    item.AlternateRelays = _graph.EdgesOf(node.Id)
                        .Select(edge => edge.OtherEnd(node.Id))
                        .Where(reached.ContainsKey)
                        .Order(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
                else if (anyGateway.TryGetValue(node.Id, out var elsewhere))
                {
                    item.Status = ScanNodeStatus.OtherGateway;
                    item.Depth = elsewhere.Depth;
                    item.HopVia = elsewhere.Parent;
                }
                else
                {
                    item.Status = ScanNodeStatus.CutOff;
                    item.BlockedBy = DownNodesAround(node.Id, CanEnter);
                }

                scan.Nodes.Add(item);
            }

            // Where to hop: live nodes whose designed relay is down.
            foreach (var item in scan.Nodes.Where(item => item.Status != ScanNodeStatus.Down))
            {
                if (!_designed.Parent.TryGetValue(item.NodeId, out var designedRelay) || IsUp(designedRelay))
                {
                    continue;
                }

                var hopsTo = item.Status == ScanNodeStatus.OtherGateway
                    ? anyGateway[item.NodeId].Depth
                    : item.Depth;
                var target = item.Status == ScanNodeStatus.OtherGateway ? "the other gateway" : rootLabel == AllGateways ? "a gateway" : rootLabel;

                scan.HopAdvice.Add(new HopAdvice
                {
                    NodeId = item.NodeId,
                    DownRelay = designedRelay,
                    HopVia = item.HopVia,
                    HopsToRoot = item.Status == ScanNodeStatus.CutOff ? null : hopsTo,
                    Advice = item.Status == ScanNodeStatus.CutOff
                        ? $"{designedRelay} is down and no live path remains. Restore {string.Join(" or ", item.BlockedBy.DefaultIfEmpty(designedRelay))}."
                        : $"{designedRelay} is down: hop via {item.HopVia} instead, {hopsTo} hop(s) to {target}" +
                          (algorithm == TraversalAlgorithm.Dfs && item.Status == ScanNodeStatus.Connected ? " (DFS path — BFS finds the fewest hops)." : ".")
                });
            }

            scan.Nodes = scan.Nodes
                .OrderBy(item => item.VisitOrder ?? int.MaxValue)
                .ThenBy(item => item.Status)
                .ThenBy(item => item.NodeId, StringComparer.OrdinalIgnoreCase)
                .ToList();

            scan.ConnectedCount = scan.Nodes.Count(item => item.Status == ScanNodeStatus.Connected);
            scan.DownCount = scan.Nodes.Count(item => item.Status == ScanNodeStatus.Down);
            scan.OtherGatewayCount = scan.Nodes.Count(item => item.Status == ScanNodeStatus.OtherGateway);
            scan.CutOffCount = scan.Nodes.Count(item => item.Status == ScanNodeStatus.CutOff);
            scan.MaxDepth = scan.Nodes.Where(item => item.Status == ScanNodeStatus.Connected).Select(item => item.Depth ?? 0).DefaultIfEmpty(0).Max();
            scan.TraversalLinkIds = traversalLinks.Order(StringComparer.OrdinalIgnoreCase).ToList();

            var name = algorithm == TraversalAlgorithm.Dfs ? "DFS" : "BFS";
            var from = rootLabel == AllGateways ? "every gateway" : rootLabel;
            var where = zoneFilter is null ? "the mesh" : zoneFilter;
            scan.Message = !scan.RootOnline
                ? $"{from} is down: no device can reach it. {scan.OtherGatewayCount} device(s) in {where} are served by another gateway."
                : $"{name} from {from} reached {scan.ConnectedCount} of {scan.InScope} device(s) in {where}" +
                  $" (max depth {scan.MaxDepth}); {scan.DownCount} down, {scan.CutOffCount} cut off" +
                  (scan.OtherGatewayCount > 0 ? $", {scan.OtherGatewayCount} on another gateway." : ".");

            return scan;
        }
    }

    /// <summary>The down nodes bordering the live cluster that <paramref name="nodeId"/> is stranded in.</summary>
    private List<string> DownNodesAround(string nodeId, Func<string, bool> canEnter) =>
        _graph.BreadthFirst([nodeId], canEnter)
            .SelectMany(visit => _graph.EdgesOf(visit.NodeId).Select(edge => edge.OtherEnd(visit.NodeId)))
            .Where(id => !IsUp(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

    // =====================================================================
    // Graph construction [27], [30]
    // =====================================================================

    /// <summary>
    /// Builds the graph on first use, and again whenever the registry has grown
    /// (page 1 registered a sensor). Not done in the constructor because the
    /// store is seeded after the container is built. Caller holds the lock.
    /// </summary>
    private void EnsureReady()
    {
        if (_store.SensorProfiles.Count != _registeredProfileCount)
        {
            BuildGraph();
        }
    }

    /// <summary>
    /// Places every node, then links each one to its nearest neighbours in
    /// radio range. Positions and link metrics are derived from each node's id,
    /// so the same registry always produces the same mesh. Caller holds the lock.
    /// </summary>
    private void BuildGraph()
    {
        _graph.Clear();
        _nodesByMac.Clear();

        var profiles = _store.SensorProfiles.ToList();
        _registeredProfileCount = profiles.Count;

        foreach (var gateway in Gateways)
        {
            AddNode(new NodeInfo
            {
                Id = gateway.Id,
                Name = gateway.Name,
                MacAddress = gateway.Mac,
                Kind = MeshNodeKind.Gateway,
                Zone = "Backbone",
                Room = "Core",
                X = gateway.X,
                Y = gateway.Y
            });
        }

        // ---- Layout: zone clusters on a ring around the gateways ----
        var zones = profiles.Select(profile => profile.Zone)
            .Where(zone => !string.IsNullOrWhiteSpace(zone))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(zone => zone, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var sensors = new List<NodeInfo>();
        foreach (var profile in profiles.OrderBy(profile => profile.NodeId, StringComparer.OrdinalIgnoreCase))
        {
            var zoneIndex = Math.Max(0, zones.FindIndex(zone => string.Equals(zone, profile.Zone, StringComparison.OrdinalIgnoreCase)));
            var angle = -Math.PI / 2 + zoneIndex * 2 * Math.PI / Math.Max(1, zones.Count);
            var random = new Random(StableHash(profile.NodeId) ^ _options.RandomSeed);

            // Square root keeps the scatter uniform over the cluster's area.
            var radius = ZoneClusterRadius * Math.Sqrt(random.NextDouble());
            var theta = random.NextDouble() * 2 * Math.PI;

            var info = new NodeInfo
            {
                Id = profile.NodeId,
                Profile = profile,
                Name = profile.Name,
                MacAddress = profile.MacAddress,
                Kind = MeshNodeKind.Sensor,
                Zone = profile.Zone,
                Room = profile.Room,
                X = 50 + ZoneRingRadius * Math.Cos(angle) + radius * Math.Cos(theta),
                Y = 50 + ZoneRingRadius * Math.Sin(angle) + radius * Math.Sin(theta)
            };

            sensors.Add(info);
        }

        SpreadOut(sensors);
        sensors.ForEach(AddNode);

        // ---- Links: nearest neighbours in range ----
        foreach (var sensor in sensors)
        {
            var nearest = sensors
                .Where(other => !ReferenceEquals(other, sensor))
                .Select(other => (Node: other, Distance: Distance(sensor, other)))
                .Where(candidate => candidate.Distance <= RadioRange)
                .OrderBy(candidate => candidate.Distance)
                .Take(MaxNeighbours);

            foreach (var (other, distance) in nearest)
            {
                AddLink(sensor, other, distance, MeshLinkKind.Radio);
            }
        }

        foreach (var gateway in _graph.Nodes.Where(node => node.Kind == MeshNodeKind.Gateway).ToList())
        {
            var nearest = sensors
                .Select(sensor => (Node: sensor, Distance: Distance(gateway, sensor)))
                .Where(candidate => candidate.Distance <= GatewayRange)
                .OrderBy(candidate => candidate.Distance)
                .Take(MaxGatewayLinks);

            foreach (var (sensor, distance) in nearest)
            {
                AddLink(gateway, sensor, distance, MeshLinkKind.GatewayUplink);
            }
        }

        StitchIsolatedClusters();

        // Designed routes: everything up, no penalties — what a reroute is measured against.
        _designed = RunDijkstra(_ => true, _ => false);

        // A rebuilt graph can change routes even if no node changed state.
        _healthSignature = null;
    }

    /// <summary>
    /// Any cluster with no gateway in it gets one bridging link: the shortest
    /// link from the cluster to a node that can reach a gateway. Repeats until
    /// every node has at least one path (a planned range extender, in effect).
    /// </summary>
    private void StitchIsolatedClusters()
    {
        while (true)
        {
            var components = _graph.Components();
            var anchored = components.Where(component => component.Any(id => _graph[id].Kind == MeshNodeKind.Gateway)).SelectMany(id => id).ToList();
            var orphan = components.FirstOrDefault(component => component.All(id => _graph[id].Kind != MeshNodeKind.Gateway));

            if (orphan is null || anchored.Count == 0)
            {
                return;
            }

            var best = orphan
                .SelectMany(id => anchored.Select(other => (From: _graph[id], To: _graph[other])))
                .Select(pair => (pair.From, pair.To, Distance: Distance(pair.From, pair.To)))
                .OrderBy(pair => pair.Distance)
                .First();

            AddLink(best.From, best.To, best.Distance,
                best.To.Kind == MeshNodeKind.Gateway ? MeshLinkKind.GatewayUplink : MeshLinkKind.Radio);
        }
    }

    private void AddNode(NodeInfo node)
    {
        _graph.AddNode(node.Id, node);
        if (MacAddress.Normalise(node.MacAddress) is string mac)
        {
            _nodesByMac[mac] = node;
        }
    }

    /// <summary>
    /// The link metrics come from a simple path-loss model: signal weakens with
    /// distance, the delivery ratio falls with the signal, and the weight is
    /// ETX = 1 / delivery ratio [30] — a perfect link costs 1, a link that needs
    /// two tries per frame costs 2.
    /// </summary>
    private void AddLink(NodeInfo a, NodeInfo b, double distance, MeshLinkKind kind)
    {
        var id = GraphEdge<LinkInfo>.MakeId(a.Id, b.Id);
        var jitter = (StableHash(id) % 600) / 100.0 - 3.0;
        var metres = distance * MetresPerUnit;

        var rssi = -40 - 1.05 * metres + jitter;
        var delivery = Math.Clamp(1 - Math.Max(0, -rssi - 55) / 45, 0.3, 0.99);
        var etx = Math.Round(1 / delivery, 3);
        var latency = Math.Round(2.5 + 4 * etx + metres * 0.05, 2);

        _graph.AddEdge(a.Id, b.Id, etx, new LinkInfo(kind, Math.Round(metres, 1), Math.Round(rssi, 1), Math.Round(delivery, 3), latency));
    }

    /// <summary>Pushes overlapping nodes apart a little at a time, so labels do not sit on top of each other.</summary>
    private static void SpreadOut(List<NodeInfo> sensors)
    {
        for (var iteration = 0; iteration < LayoutIterations; iteration++)
        {
            for (var i = 0; i < sensors.Count; i++)
            {
                for (var j = i + 1; j < sensors.Count; j++)
                {
                    var a = sensors[i];
                    var b = sensors[j];
                    var dx = b.X - a.X;
                    var dy = b.Y - a.Y;
                    var distance = Math.Sqrt(dx * dx + dy * dy);

                    if (distance >= MinNodeSpacing)
                    {
                        continue;
                    }

                    if (distance < 0.01)
                    {
                        dx = 0.5;
                        dy = 0.3;
                        distance = Math.Sqrt(dx * dx + dy * dy);
                    }

                    var push = (MinNodeSpacing - distance) / 2 / distance;
                    a.X -= dx * push;
                    a.Y -= dy * push;
                    b.X += dx * push;
                    b.Y += dy * push;
                }

                foreach (var gateway in Gateways)
                {
                    var dx = sensors[i].X - gateway.X;
                    var dy = sensors[i].Y - gateway.Y;
                    var distance = Math.Sqrt(dx * dx + dy * dy);
                    if (distance is > 0.01 and < MinNodeSpacing * 1.5)
                    {
                        var push = (MinNodeSpacing * 1.5 - distance) / distance;
                        sensors[i].X += dx * push;
                        sensors[i].Y += dy * push;
                    }
                }
            }
        }

        foreach (var sensor in sensors)
        {
            sensor.X = Math.Round(Math.Clamp(sensor.X, LayoutMargin, 100 - LayoutMargin), 2);
            sensor.Y = Math.Round(Math.Clamp(sensor.Y, LayoutMargin, 100 - LayoutMargin), 2);
        }
    }

    // =====================================================================
    // Health and recompute
    // =====================================================================

    /// <summary>
    /// Reads which nodes are down and which are degraded, and recomputes every
    /// route and the spanning tree if that picture has changed (or when forced).
    /// Caller holds the lock.
    /// </summary>
    private void RecomputeIfChanged(bool force)
    {
        var disconnected = new HashSet<string>(_commands.GetDisconnectedNodeIds(), StringComparer.OrdinalIgnoreCase);
        var down = new Dictionary<string, OfflineReason>(StringComparer.OrdinalIgnoreCase);
        var degraded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in _graph.Nodes)
        {
            if (_simulatedFailures.Contains(node.Id))
            {
                down[node.Id] = OfflineReason.SimulatedOutage;
            }
            else if (node.Profile is SensorProfile profile)
            {
                if (!profile.IsActive)
                {
                    down[node.Id] = OfflineReason.Deactivated;
                }
                else if (disconnected.Contains(node.Id))
                {
                    down[node.Id] = OfflineReason.LinkLost;
                }
                else if (profile.Status == SensorStatus.Warning)
                {
                    degraded.Add(node.Id);
                }
            }
        }

        var signature = string.Join(',', down.Keys.Order(StringComparer.OrdinalIgnoreCase)) + "|" +
                        string.Join(',', degraded.Order(StringComparer.OrdinalIgnoreCase));

        if (!force && signature == _healthSignature)
        {
            return;
        }

        _down = down;
        _degraded = degraded;
        _healthSignature = signature;

        var started = Stopwatch.GetTimestamp();
        var routes = RunDijkstra(IsUp, _degraded.Contains);
        _dijkstraMicroseconds = Math.Round(Stopwatch.GetElapsedTime(started).TotalMicroseconds, 2);

        started = Stopwatch.GetTimestamp();
        _mst = RunPrim(IsUp);
        _primMicroseconds = Math.Round(Stopwatch.GetElapsedTime(started).TotalMicroseconds, 2);

        started = Stopwatch.GetTimestamp();
        var (kruskalEdges, kruskalCost) = RunKruskal(IsUp);
        _kruskal = (kruskalEdges, kruskalCost, Math.Round(Stopwatch.GetElapsedTime(started).TotalMicroseconds, 2));

        var previous = _current;
        _current = routes;
        _lastRecomputedUtc = DateTime.UtcNow;
        RecordRouteSnapshots();

        if (_graph.NodeIds.Any(id => !SamePath(PathOf(id, previous), PathOf(id, _current))))
        {
            _routeVersion++;
        }
    }

    private bool IsUp(string nodeId) => !_down.ContainsKey(nodeId);

    // =====================================================================
    // Shortest paths — Dijkstra [28] over a binary heap [26]
    // =====================================================================

    /// <summary>
    /// Multi-source Dijkstra: every live gateway starts at cost 0, so each node
    /// settles on its cheapest path to whichever gateway is nearest. A node's
    /// parent is the neighbour it forwards to. Nodes <paramref name="isUp"/>
    /// rejects are never relaxed, so routes flow around them — the self-healing.
    /// <para>
    /// The heap holds (cost, hops) so equal-cost paths prefer fewer hops. Stale
    /// heap entries are skipped when popped (lazy deletion) rather than
    /// decreased in place: O((V + E) log V) overall.
    /// </para>
    /// </summary>
    private RoutingState RunDijkstra(Func<string, bool> isUp, Func<string, bool> isDegraded)
    {
        var state = new RoutingState();
        var heap = new BinaryHeap<string, (double Cost, int Hops)>(_graph.NodeCount);
        var settled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var gateway in _graph.Nodes.Where(node => node.Kind == MeshNodeKind.Gateway && isUp(node.Id)))
        {
            state.Cost[gateway.Id] = 0;
            state.Hops[gateway.Id] = 0;
            state.Gateway[gateway.Id] = gateway.Id;
            heap.Enqueue(gateway.Id, (0, 0));
        }

        while (heap.TryDequeue(out var nodeId, out var priority))
        {
            // Lazy deletion: a cheaper path to this node was already settled.
            if (!settled.Add(nodeId) || priority.Cost > state.Cost[nodeId] + Epsilon)
            {
                continue;
            }

            var node = _graph[nodeId];

            // Relaying through a degraded sensor costs extra; a gateway never does.
            var penalty = node.Kind == MeshNodeKind.Sensor && isDegraded(nodeId) ? DegradedRelayPenalty : 0;

            foreach (var edge in _graph.EdgesOf(nodeId))
            {
                var next = edge.OtherEnd(nodeId);
                if (settled.Contains(next) || !isUp(next) || _graph[next].Kind == MeshNodeKind.Gateway)
                {
                    continue;
                }

                var cost = state.Cost[nodeId] + edge.Weight + penalty;
                var hops = state.Hops[nodeId] + 1;

                var better = !state.Cost.TryGetValue(next, out var known)
                    || cost < known - Epsilon
                    || (Math.Abs(cost - known) <= Epsilon && hops < state.Hops[next]);

                if (!better)
                {
                    continue;
                }

                state.Cost[next] = cost;
                state.Hops[next] = hops;
                state.Parent[next] = nodeId;
                state.Gateway[next] = state.Gateway[nodeId];
                heap.Enqueue(next, (cost, hops));
            }
        }

        BuildRoutingTree(state);
        return state;
    }

    // =====================================================================
    // Routing tree [32], walked recursively [12]
    // =====================================================================

    /// <summary>
    /// Turns the parent pointers into a tree per gateway, then walks it
    /// recursively to count how many nodes sit below each relay.
    /// </summary>
    private void BuildRoutingTree(RoutingState state)
    {
        foreach (var nodeId in state.Cost.Keys)
        {
            state.TreeIndex[nodeId] = new RoutingTreeNode
            {
                NodeId = nodeId,
                Kind = _graph[nodeId].Kind,
                Depth = state.Hops[nodeId],
                PathCost = Math.Round(state.Cost[nodeId], 3)
            };
        }

        foreach (var (child, parent) in state.Parent)
        {
            state.TreeIndex[parent].Children.Add(state.TreeIndex[child]);
        }

        foreach (var treeNode in state.TreeIndex.Values)
        {
            treeNode.Children.Sort((left, right) => string.Compare(left.NodeId, right.NodeId, StringComparison.OrdinalIgnoreCase));
        }

        state.Roots = state.TreeIndex.Values
            .Where(treeNode => treeNode.Kind == MeshNodeKind.Gateway)
            .OrderBy(treeNode => treeNode.NodeId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var root in state.Roots)
        {
            CountDependants(root, 0);
        }
    }

    /// <summary>
    /// Post-order recursion: a node's subtree size is the sum of its children's
    /// subtree sizes plus the children themselves. The depth guard stops a
    /// malformed tree from overflowing the stack.
    /// </summary>
    private static int CountDependants(RoutingTreeNode node, int depth)
    {
        if (depth > MaxTreeDepth)
        {
            return 0;
        }

        var total = 0;
        foreach (var child in node.Children)
        {
            total += 1 + CountDependants(child, depth + 1);
        }

        node.SubtreeSize = total;
        return total;
    }

    /// <summary>Every node below <paramref name="node"/>, collected recursively.</summary>
    private static List<string> CollectSubtree(RoutingTreeNode node, int depth)
    {
        var collected = new List<string>();
        if (depth > MaxTreeDepth)
        {
            return collected;
        }

        foreach (var child in node.Children)
        {
            collected.Add(child.NodeId);
            collected.AddRange(CollectSubtree(child, depth + 1));
        }

        return collected;
    }

    // =====================================================================
    // Minimum spanning tree — Prim [29] over a binary heap [26]
    // =====================================================================

    /// <summary>
    /// Grows the tree from every live gateway at once: repeatedly takes the
    /// cheapest link from a node already connected to one that is not. The
    /// result is the cheapest set of links that keeps every reachable live node
    /// connected to a gateway — the most power-efficient backbone the mesh could
    /// fall back to. O(E log E).
    /// </summary>
    private SpanningForest RunPrim(Func<string, bool> isUp)
    {
        var forest = new SpanningForest();
        var inTree = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var heap = new BinaryHeap<(GraphEdge<LinkInfo> Edge, string From), double>(_graph.EdgeCount + 1);

        void Grow(string nodeId)
        {
            inTree.Add(nodeId);
            foreach (var edge in _graph.EdgesOf(nodeId))
            {
                var next = edge.OtherEnd(nodeId);
                if (!inTree.Contains(next) && isUp(next) && _graph[next].Kind != MeshNodeKind.Gateway)
                {
                    heap.Enqueue((edge, nodeId), edge.Weight);
                }
            }
        }

        foreach (var gateway in _graph.Nodes.Where(node => node.Kind == MeshNodeKind.Gateway && isUp(node.Id)))
        {
            Grow(gateway.Id);
        }

        while (heap.TryDequeue(out var candidate, out var weight))
        {
            var next = candidate.Edge.OtherEnd(candidate.From);
            if (inTree.Contains(next))
            {
                // Both ends already connected: taking it would close a cycle.
                continue;
            }

            forest.EdgeIds.Add(candidate.Edge.Id);
            forest.Parent[next] = candidate.From;
            forest.TotalCost += weight;
            Grow(next);
        }

        forest.TotalCost = Math.Round(forest.TotalCost, 3);
        return forest;
    }

    /// <summary>
    /// Kruskal [35]: take the live links cheapest first and keep each one that
    /// joins two different trees, using a disjoint-set to spot a cycle. The live
    /// gateways start joined into one root (as Prim grows from all of them at
    /// once), and trees never joined to that root — clusters cut off by a
    /// failure — are left out, as Prim leaves them out. The two builds must then
    /// agree on cost; the summary reports whether they do. O(E log E).
    /// </summary>
    private (int Edges, double Cost) RunKruskal(Func<string, bool> isUp)
    {
        const string root = "⊤GATEWAYS";
        var sets = new DisjointSet();
        foreach (var gateway in _graph.Nodes.Where(node => node.Kind == MeshNodeKind.Gateway && isUp(node.Id)))
        {
            sets.Union(root, gateway.Id);
        }

        var chosen = new List<GraphEdge<LinkInfo>>();
        foreach (var edge in _graph.Edges
                     .Where(edge => isUp(edge.Either) && isUp(edge.Other))
                     .OrderBy(edge => edge.Weight)
                     .ThenBy(edge => edge.Id, StringComparer.OrdinalIgnoreCase))
        {
            if (sets.Union(edge.Either, edge.Other))
            {
                chosen.Add(edge);
            }
        }

        var anchored = chosen.Where(edge => sets.Connected(edge.Either, root)).ToList();
        return (anchored.Count, Math.Round(anchored.Sum(edge => edge.Weight), 3));
    }

    // =====================================================================
    // Route history — SortedList [18]
    // =====================================================================

    /// <summary>
    /// Appends a snapshot for every node whose route differs from its last one.
    /// The very first pass also records the designed route as the state before
    /// the engine started, so archived readings get the route they were sent on.
    /// </summary>
    private void RecordRouteSnapshots()
    {
        var now = DateTime.UtcNow;

        foreach (var node in _graph.Nodes.Where(node => node.Kind == MeshNodeKind.Sensor))
        {
            if (!_routeHistory.TryGetValue(node.Id, out var history))
            {
                history = new SortedList<DateTime, RouteSnapshot>();
                _routeHistory[node.Id] = history;
                history.Add(DateTime.MinValue, SnapshotOf(node.Id, _designed));
            }

            var snapshot = SnapshotOf(node.Id, _current);
            if (history.Values[^1] == snapshot)
            {
                continue;
            }

            var key = now;
            while (history.ContainsKey(key))
            {
                key = key.AddTicks(1);
            }

            history.Add(key, snapshot);

            while (history.Count > MaxRouteSnapshotsPerNode)
            {
                history.RemoveAt(0);
            }
        }
    }

    private static RouteSnapshot SnapshotOf(string nodeId, RoutingState state) =>
        state.Cost.ContainsKey(nodeId)
            ? new RouteSnapshot(state.Hops[nodeId], state.Gateway[nodeId], state.Parent.GetValueOrDefault(nodeId))
            : new RouteSnapshot(null, null, null);

    /// <summary>The snapshot in effect at <paramref name="atUtc"/>: binary search for the last key at or before it.</summary>
    private RouteSnapshot? SnapshotAt(string nodeId, DateTime atUtc)
    {
        if (!_routeHistory.TryGetValue(nodeId, out var history) || history.Count == 0)
        {
            return null;
        }

        var keys = history.Keys;
        var low = 0;
        var high = keys.Count;

        // First key strictly after atUtc; the one before it is in effect.
        while (low < high)
        {
            var middle = low + (high - low) / 2;
            if (keys[middle] <= atUtc)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return history.Values[Math.Max(0, low - 1)];
    }

    private List<RouteChange> RouteChangesBetween(string nodeId, DateTime from, DateTime to)
    {
        var changes = new List<RouteChange>();
        if (!_routeHistory.TryGetValue(nodeId, out var history))
        {
            return changes;
        }

        for (var index = 1; index < history.Count; index++)
        {
            var at = history.Keys[index];
            if (at < from || at > to)
            {
                continue;
            }

            var before = history.Values[index - 1];
            var after = history.Values[index];
            changes.Add(new RouteChange
            {
                TimestampUtc = at,
                HopsBefore = before.Hops,
                HopsAfter = after.Hops,
                GatewayAfter = after.GatewayId,
                Description = after.Hops is null
                    ? "Lost its route to the gateway"
                    : before.Hops is null
                        ? $"Route restored: {after.Hops} hop(s) via {after.GatewayId}"
                        : before.Hops == after.Hops
                            ? $"Re-routed via {after.NextHop}, still {after.Hops} hop(s) to {after.GatewayId}"
                            : $"Re-routed via {after.NextHop}: {before.Hops} → {after.Hops} hop(s) to {after.GatewayId}"
            });
        }

        return changes;
    }

    // =====================================================================
    // History helpers — AVL trees [33], [34]
    // =====================================================================

    /// <summary>
    /// The sensor's stored readings, one AVL tree per metric keyed by timestamp.
    /// The store only appends, so readings added since the last call are
    /// inserted (O(log n) each) rather than the trees being rebuilt.
    /// </summary>
    private SensorArchive ArchiveFor(SensorProfile profile, IReadOnlyDictionary<ReadingType, (double Min, double Max, bool IsBoolean)> limits)
    {
        var readings = _store.ReadingsFor(profile.Id);
        if (!_archive.TryGetValue(profile.Id, out var archive) || readings.Count < archive.Indexed)
        {
            // First query, or the store was reseeded: index from scratch.
            archive = new SensorArchive();
            _archive[profile.Id] = archive;
        }

        if (archive.Indexed == readings.Count)
        {
            return archive;
        }

        var snapshot = readings.ToArray();
        for (var index = archive.Indexed; index < snapshot.Length; index++)
        {
            var reading = snapshot[index];
            var value = reading.NumericValue ?? (reading.BooleanValue is bool flag ? (flag ? 1 : 0) : null);
            if (value is null)
            {
                continue;
            }

            if (!archive.Trees.TryGetValue(reading.ReadingType, out var tree))
            {
                tree = new AvlTree<DateTime, TelemetryHistoryEntry>();
                archive.Trees[reading.ReadingType] = tree;
            }

            // Add refuses a key the tree already holds (same time, same metric), so a replayed batch cannot double up.
            tree.Add(reading.TimestampUtc, ToHistoryEntry(reading.TimestampUtc, reading.ReadingType, value.Value, reading.Quality, reading.IsAnomaly, HistorySource.Archive, limits));
        }

        archive.Indexed = snapshot.Length;
        return archive;
    }

    /// <summary>
    /// K-way merge of the per-metric range results (each already in time order)
    /// into one time-ordered list, through the binary heap [26]: the heap holds
    /// the head of each list, so each step is O(log k).
    /// </summary>
    private static List<TelemetryHistoryEntry> MergeByTime(List<List<TelemetryHistoryEntry>> lists)
    {
        if (lists.Count == 1)
        {
            return lists[0];
        }

        var merged = new List<TelemetryHistoryEntry>(lists.Sum(list => list.Count));
        var heap = new BinaryHeap<(int List, int Index), TelemetryHistoryEntry>(Math.Max(1, lists.Count), HistoryOrder.Instance);

        for (var list = 0; list < lists.Count; list++)
        {
            if (lists[list].Count > 0)
            {
                heap.Enqueue((list, 0), lists[list][0]);
            }
        }

        while (heap.TryDequeue(out var head, out var entry))
        {
            merged.Add(entry);
            var next = head.Index + 1;
            if (next < lists[head.List].Count)
            {
                heap.Enqueue((head.List, next), lists[head.List][next]);
            }
        }

        return merged;
    }

    /// <summary>
    /// One linear merge of two time-ordered sequences. Where both hold the same
    /// reading (the live log is back-filled from the archive) the archive copy
    /// is kept, because it carries the quality flags. A repeated live packet
    /// (same time and metric) is yielded once.
    /// </summary>
    private static IEnumerable<TelemetryHistoryEntry> MergeSorted(IEnumerable<TelemetryHistoryEntry> archive, List<TelemetryHistoryEntry> live)
    {
        var order = HistoryOrder.Instance;
        TelemetryHistoryEntry? last = null;
        var liveIndex = 0;

        foreach (var stored in archive)
        {
            while (liveIndex < live.Count && order.Compare(live[liveIndex], stored) < 0)
            {
                var entry = live[liveIndex++];
                if (last is null || order.Compare(last, entry) != 0)
                {
                    yield return last = entry;
                }
            }

            while (liveIndex < live.Count && order.Compare(live[liveIndex], stored) == 0)
            {
                liveIndex++;
            }

            yield return last = stored;
        }

        while (liveIndex < live.Count)
        {
            var entry = live[liveIndex++];
            if (last is null || order.Compare(last, entry) != 0)
            {
                yield return last = entry;
            }
        }
    }

    private static TelemetryHistoryEntry ToHistoryEntry(
        DateTime timestampUtc,
        ReadingType readingType,
        double value,
        ReadingQuality quality,
        bool isAnomaly,
        HistorySource source,
        IReadOnlyDictionary<ReadingType, (double Min, double Max, bool IsBoolean)> limits)
    {
        var outOfRange = limits.TryGetValue(readingType, out var limit) && !limit.IsBoolean && (value < limit.Min || value > limit.Max);

        return new TelemetryHistoryEntry
        {
            TimestampUtc = timestampUtc,
            ReadingType = readingType,
            Value = Math.Round(value, 3),
            Unit = ReadingTypeProfile.For(readingType).Unit,
            Source = source,
            Status = quality switch
            {
                ReadingQuality.Bad => HistoryEntryStatus.Bad,
                ReadingQuality.Suspect => HistoryEntryStatus.Suspect,
                _ when isAnomaly || outOfRange => HistoryEntryStatus.Breach,
                _ => HistoryEntryStatus.Normal
            }
        };
    }

    /// <summary>The sensor's configured limits per metric, falling back to the reading type's defaults.</summary>
    private Dictionary<ReadingType, (double Min, double Max, bool IsBoolean)> LimitsFor(SensorProfile profile)
    {
        var limits = new Dictionary<ReadingType, (double Min, double Max, bool IsBoolean)>();
        foreach (var readingType in ReadingTypeProfile.ForCategory(profile.Category))
        {
            var defaults = ReadingTypeProfile.For(readingType);
            var threshold = _store.SensorThresholds.FirstOrDefault(item =>
                item.SensorProfileId == profile.Id && item.ReadingType == readingType && item.IsEnabled);

            limits[readingType] = (
                threshold?.MinValue ?? defaults.MinThreshold,
                threshold?.MaxValue ?? defaults.MaxThreshold,
                defaults.IsBoolean);
        }

        return limits;
    }

    private static List<HistoryMetricSummary> Summarise(List<TelemetryHistoryEntry> entries) =>
        entries
            .GroupBy(entry => entry.ReadingType)
            .OrderBy(group => group.Key)
            .Select(group => new HistoryMetricSummary
            {
                ReadingType = group.Key,
                Unit = ReadingTypeProfile.For(group.Key).Unit,
                Count = group.Count(),
                Min = Math.Round(group.Min(entry => entry.Value), 3),
                Max = Math.Round(group.Max(entry => entry.Value), 3),
                Average = Math.Round(group.Average(entry => entry.Value), 3),
                Breaches = group.Count(entry => entry.Status == HistoryEntryStatus.Breach)
            })
            .ToList();

    /// <summary>Page items get their route filled in, so they are copied rather than mutated inside the cached set.</summary>
    private static TelemetryHistoryEntry Clone(TelemetryHistoryEntry entry) => new()
    {
        TimestampUtc = entry.TimestampUtc,
        ReadingType = entry.ReadingType,
        Value = entry.Value,
        Unit = entry.Unit,
        Status = entry.Status,
        Source = entry.Source
    };

    // =====================================================================
    // Projections
    // =====================================================================

    /// <summary>Node id first, then MAC address in any notation — both O(1) dictionary probes [17].</summary>
    private (NodeInfo? Node, string? MatchedBy, string Normalised) Resolve(string key)
    {
        if (_graph.TryGetNode(key, out var byId))
        {
            return (byId, "NodeId", byId.Id);
        }

        if (MacAddress.Normalise(key) is string mac)
        {
            return _nodesByMac.TryGetValue(mac, out var byMac)
                ? (byMac, "MacAddress", mac)
                : (null, null, mac);
        }

        return (null, null, key);
    }

    private MeshTopology BuildTopology()
    {
        var nodes = _graph.Nodes
            .OrderBy(node => node.Kind)
            .ThenBy(node => node.Id, StringComparer.OrdinalIgnoreCase)
            .Select(ToMeshNode)
            .ToList();

        var treeLinks = new HashSet<string>(
            _current.Parent.Select(pair => GraphEdge<LinkInfo>.MakeId(pair.Key, pair.Value)),
            StringComparer.OrdinalIgnoreCase);

        var links = _graph.Edges
            .OrderBy(edge => edge.Id, StringComparer.OrdinalIgnoreCase)
            .Select(edge => new MeshLink
            {
                Id = edge.Id,
                From = edge.Either,
                To = edge.Other,
                Kind = edge.Data.Kind,
                DistanceMetres = edge.Data.DistanceMetres,
                RssiDbm = edge.Data.RssiDbm,
                DeliveryRatio = edge.Data.DeliveryRatio,
                Cost = edge.Weight,
                LatencyMs = edge.Data.LatencyMs,
                IsActive = IsUp(edge.Either) && IsUp(edge.Other),
                InMst = _mst.EdgeIds.Contains(edge.Id),
                InRoutingTree = treeLinks.Contains(edge.Id)
            })
            .ToList();

        var sensors = nodes.Where(node => node.Kind == MeshNodeKind.Sensor).ToList();
        var routed = sensors.Where(node => node.Hops.HasValue).ToList();
        var activeLinks = links.Where(link => link.IsActive).ToList();

        return new MeshTopology
        {
            Nodes = nodes,
            Links = links,
            Zones = sensors.Select(node => node.Zone).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToList(),
            CriticalRelays = sensors
                .Where(node => node.Dependants > 0)
                .OrderByDescending(node => node.Dependants)
                .ThenBy(node => node.Id, StringComparer.OrdinalIgnoreCase)
                .Take(CriticalRelayCount)
                .Select(node => new CriticalRelay { NodeId = node.Id, Name = node.Name, Zone = node.Zone, Dependants = node.Dependants })
                .ToList(),
            Summary = new TopologySummary
            {
                SensorNodes = sensors.Count,
                ActiveNodes = sensors.Count(node => node.State is MeshNodeState.Online or MeshNodeState.Degraded),
                DegradedNodes = sensors.Count(node => node.State == MeshNodeState.Degraded),
                OfflineNodes = sensors.Count(node => node.State == MeshNodeState.Offline),
                IsolatedNodes = sensors.Count(node => node.State == MeshNodeState.Isolated),
                ReroutedNodes = sensors.Count(node => node.IsRerouted),
                Gateways = nodes.Count(node => node.Kind == MeshNodeKind.Gateway),
                OnlineGateways = nodes.Count(node => node.Kind == MeshNodeKind.Gateway && node.State == MeshNodeState.Online),
                TotalLinks = links.Count,
                ActiveLinks = activeLinks.Count,
                AverageHops = routed.Count == 0 ? 0 : Math.Round(routed.Average(node => node.Hops!.Value), 2),
                MaxHops = routed.Count == 0 ? 0 : routed.Max(node => node.Hops!.Value),
                MstEdgeCount = _mst.EdgeIds.Count,
                MstTotalCost = _mst.TotalCost,
                ActiveMeshCost = Math.Round(activeLinks.Sum(link => link.Cost), 3),
                RedundantLinks = activeLinks.Count - _mst.EdgeIds.Count,
                DijkstraMicroseconds = _dijkstraMicroseconds,
                PrimMicroseconds = _primMicroseconds,
                KruskalEdgeCount = _kruskal.Edges,
                KruskalTotalCost = _kruskal.Cost,
                KruskalMicroseconds = _kruskal.Microseconds,
                MstVerified = _kruskal.Edges == _mst.EdgeIds.Count && Math.Abs(_kruskal.Cost - _mst.TotalCost) < 1e-6,
                RouteVersion = _routeVersion,
                LastRecomputedUtc = _lastRecomputedUtc
            },
            GeneratedUtc = DateTime.UtcNow
        };
    }

    private MeshNode ToMeshNode(NodeInfo node)
    {
        var routed = _current.Cost.TryGetValue(node.Id, out var cost);
        var reason = _down.GetValueOrDefault(node.Id, OfflineReason.None);

        var state = reason != OfflineReason.None
            ? MeshNodeState.Offline
            : !routed
                ? MeshNodeState.Isolated
                : _degraded.Contains(node.Id)
                    ? MeshNodeState.Degraded
                    : MeshNodeState.Online;

        return new MeshNode
        {
            Id = node.Id,
            SensorProfileId = node.Profile?.Id,
            Name = node.Name,
            MacAddress = node.MacAddress,
            Kind = node.Kind,
            Category = node.Profile?.Category,
            Zone = node.Zone,
            Room = node.Room,
            X = Math.Round(node.X, 2),
            Y = Math.Round(node.Y, 2),
            State = state,
            OfflineReason = reason,
            IsSimulatedFailure = _simulatedFailures.Contains(node.Id),
            Degree = _graph.Degree(node.Id),
            Hops = routed ? _current.Hops[node.Id] : null,
            PathCost = routed ? Math.Round(cost, 3) : null,
            GatewayId = routed ? _current.Gateway[node.Id] : null,
            NextHop = _current.Parent.GetValueOrDefault(node.Id),
            Dependants = _current.TreeIndex.TryGetValue(node.Id, out var treeNode) ? treeNode.SubtreeSize : 0,
            IsRerouted = node.Kind == MeshNodeKind.Sensor && IsRerouted(node.Id)
        };
    }

    /// <summary>A node is re-routed when it is reachable but not on the path it would use with every node up.</summary>
    private bool IsRerouted(string nodeId) =>
        _current.Cost.ContainsKey(nodeId) && !SamePath(PathOf(nodeId, _current), PathOf(nodeId, _designed));

    /// <summary>Source first, gateway last; empty when the node has no route.</summary>
    private static List<string> PathOf(string nodeId, RoutingState state)
    {
        var path = new List<string>();
        if (!state.Cost.ContainsKey(nodeId))
        {
            return path;
        }

        var current = nodeId;
        path.Add(current);
        while (state.Parent.TryGetValue(current, out var parent) && path.Count <= MaxTreeDepth)
        {
            path.Add(parent);
            current = parent;
        }

        return path;
    }

    private static bool SamePath(List<string> left, List<string> right) =>
        left.SequenceEqual(right, StringComparer.OrdinalIgnoreCase);

    private RoutePath? BuildPath(string nodeId, RoutingState state)
    {
        var path = PathOf(nodeId, state);
        return path.Count == 0 ? null : ToRoutePath(path);
    }

    /// <summary>The node's path along the spanning tree, following its MST parents up to a gateway.</summary>
    private RoutePath? BuildSpanningPath(string nodeId)
    {
        if (!IsUp(nodeId))
        {
            return null;
        }

        var path = new List<string> { nodeId };
        var current = nodeId;
        while (_mst.Parent.TryGetValue(current, out var parent) && path.Count <= MaxTreeDepth)
        {
            path.Add(parent);
            current = parent;
        }

        return _graph[current].Kind == MeshNodeKind.Gateway ? ToRoutePath(path) : null;
    }

    private RoutePath ToRoutePath(List<string> path)
    {
        var route = new RoutePath
        {
            HopCount = path.Count - 1,
            GatewayId = path[^1]
        };

        var cumulative = 0d;
        for (var index = 0; index < path.Count; index++)
        {
            var node = _graph[path[index]];
            var hop = new RouteHop
            {
                Index = index,
                NodeId = node.Id,
                Name = node.Name,
                MacAddress = node.MacAddress,
                Kind = node.Kind,
                State = ToMeshNode(node).State,
                CumulativeCost = Math.Round(cumulative, 3)
            };

            if (index < path.Count - 1 && _graph.FindEdge(path[index], path[index + 1]) is { } edge)
            {
                hop.LinkCost = edge.Weight;
                hop.LinkLatencyMs = edge.Data.LatencyMs;
                hop.LinkRssiDbm = edge.Data.RssiDbm;
                route.LinkIds.Add(edge.Id);
                cumulative += edge.Weight;
                route.TotalLatencyMs += edge.Data.LatencyMs;
            }

            route.Hops.Add(hop);
        }

        route.TotalCost = Math.Round(cumulative, 3);
        route.TotalLatencyMs = Math.Round(route.TotalLatencyMs, 2);
        return route;
    }

    // =====================================================================
    // Small helpers
    // =====================================================================

    private static double Distance(NodeInfo a, NodeInfo b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// FNV-1a over the lower-cased id. string.GetHashCode is randomised per
    /// process, which would move every node on each restart.
    /// </summary>
    private static int StableHash(string value)
    {
        unchecked
        {
            var hash = 2166136261;
            foreach (var character in value.ToLowerInvariant())
            {
                hash ^= character;
                hash *= 16777619;
            }

            return (int)(hash & 0x7FFFFFFF);
        }
    }

    private static string Describe(OfflineReason reason) => reason switch
    {
        OfflineReason.LinkLost => "link lost",
        OfflineReason.Deactivated => "deactivated",
        OfflineReason.SimulatedOutage => "simulated outage",
        _ => "offline"
    };

    // =====================================================================
    // Private types
    // =====================================================================

    private sealed class NodeInfo
    {
        public string Id { get; init; } = string.Empty;

        /// <summary>The live profile, so a status change on page 1 is seen on the next pass. Null on gateways.</summary>
        public SensorProfile? Profile { get; init; }

        public string Name { get; init; } = string.Empty;
        public string MacAddress { get; init; } = string.Empty;
        public MeshNodeKind Kind { get; init; }
        public string Zone { get; init; } = string.Empty;
        public string Room { get; init; } = string.Empty;
        public double X { get; set; }
        public double Y { get; set; }
    }

    private sealed record LinkInfo(MeshLinkKind Kind, double DistanceMetres, double RssiDbm, double DeliveryRatio, double LatencyMs);

    /// <summary>One Dijkstra pass: cost, hops, next hop and gateway per reachable node, plus the routing tree.</summary>
    private sealed class RoutingState
    {
        public static readonly RoutingState Empty = new();

        public Dictionary<string, double> Cost { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> Hops { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> Parent { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> Gateway { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, RoutingTreeNode> TreeIndex { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<RoutingTreeNode> Roots { get; set; } = new();
    }

    private sealed class SpanningForest
    {
        public static readonly SpanningForest Empty = new();

        public HashSet<string> EdgeIds { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Each node's neighbour towards its gateway along the tree.</summary>
        public Dictionary<string, string> Parent { get; } = new(StringComparer.OrdinalIgnoreCase);

        public double TotalCost { get; set; }
    }

    private readonly record struct RouteSnapshot(int? Hops, string? GatewayId, string? NextHop);

    /// <summary>One sensor's indexed history: an AVL tree per metric, and how many stored readings have been inserted.</summary>
    private sealed class SensorArchive
    {
        public int Indexed { get; set; }
        public Dictionary<ReadingType, AvlTree<DateTime, TelemetryHistoryEntry>> Trees { get; } = new();
    }

    /// <summary>Time first, then metric — the order of the history set, and what makes two entries "the same reading".</summary>
    private sealed class HistoryOrder : IComparer<TelemetryHistoryEntry>
    {
        public static readonly HistoryOrder Instance = new();

        public int Compare(TelemetryHistoryEntry? left, TelemetryHistoryEntry? right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left is null) return -1;
            if (right is null) return 1;

            var byTime = left.TimestampUtc.CompareTo(right.TimestampUtc);
            return byTime != 0 ? byTime : ((int)left.ReadingType).CompareTo((int)right.ReadingType);
        }
    }
}
