// =============================================================================
// CODE ATTRIBUTION — Graphs (Part 3)
//
// The weighted, undirected adjacency-list graph below was written with
// reference to the source below.
//
// Code Attribution [27]
// Author: Sedgewick, R. and Wayne, K.
// Year: 2011
// Title: Algorithms, Section 4.3: Minimum Spanning Trees (EdgeWeightedGraph)
// Version: 4th edition
// Type: [Source code]
// Available at: <https://algs4.cs.princeton.edu/43mst/>
// Accessed: [Accessed 2 November 2026]
// Modifications: Adapted the EdgeWeightedGraph API (an Edge object shared by
//   both endpoints' adjacency lists, Other(v), Adj(v) and Edges()) to string
//   node ids held in a Dictionary, with a generic payload on each edge so the
//   graph carries the radio-link metrics, and a BFS component walk used to
//   stitch isolated clusters back onto the gateways.
// Reference: Sedgewick, R. and Wayne, K., 2011. Algorithms, Section 4.3: Minimum Spanning Trees (4th edition) [Source code] Available at: <https://algs4.cs.princeton.edu/43mst/> [Accessed 2 November 2026].
//
// Code Attribution [36]
// Author: Sedgewick, R. and Wayne, K.
// Year: 2011
// Title: Algorithms, Section 4.1: Undirected Graphs (BreadthFirstPaths, DepthFirstPaths)
// Version: 4th edition
// Type: [Source code]
// Available at: <https://algs4.cs.princeton.edu/41graph/>
// Accessed: [Accessed 6 November 2026]
// Modifications: BreadthFirst follows BreadthFirstPaths (a FIFO queue, marked
//   on enqueue, edgeTo[] kept as each visit's Parent) from several sources at
//   once. DepthFirst replaces the recursive DepthFirstPaths with an explicit
//   Stack<T>. Both take a predicate so offline nodes are never entered, and
//   both record visit order and depth for the connectivity scan.
// Reference: Sedgewick, R. and Wayne, K., 2011. Algorithms, Section 4.1: Undirected Graphs (4th edition) [Source code] Available at: <https://algs4.cs.princeton.edu/41graph/> [Accessed 6 November 2026].
// =============================================================================

namespace SmartX.Api.Data.Collections;

/// <summary>
/// One node reached by a traversal: the order it was reached in, its depth in
/// the traversal tree, and the neighbour (and edge) it was reached from.
/// </summary>
public sealed record TraversalVisit(string NodeId, int Order, int Depth, string? Parent, string? ViaEdgeId);

/// <summary>One undirected, weighted edge. Both endpoints' adjacency lists hold the same instance.</summary>
public sealed class GraphEdge<TEdge>
{
    public GraphEdge(string id, string either, string other, double weight, TEdge data)
    {
        Id = id;
        Either = either;
        Other = other;
        Weight = weight;
        Data = data;
    }

    /// <summary>Stable id: the two endpoint ids in ordinal order, joined by "|".</summary>
    public string Id { get; }

    public string Either { get; }
    public string Other { get; }

    /// <summary>The base routing cost of the edge (ETX for a radio link).</summary>
    public double Weight { get; }

    public TEdge Data { get; }

    /// <summary>The endpoint that is not <paramref name="node"/>.</summary>
    public string OtherEnd(string node) =>
        string.Equals(node, Either, StringComparison.OrdinalIgnoreCase) ? Other : Either;

    public static string MakeId(string a, string b) =>
        string.Compare(a, b, StringComparison.OrdinalIgnoreCase) <= 0 ? $"{a}|{b}" : $"{b}|{a}";
}

/// <summary>
/// The mesh as a graph: an adjacency list keyed by node id. Each node maps to
/// the edges touching it, so visiting a node's neighbours costs O(degree)
/// rather than a scan of every link — the operation Dijkstra and Prim repeat
/// for every node they settle.
/// <para>
/// Undirected: a radio link carries packets both ways, so each edge is stored
/// once and referenced from both endpoints. Not thread-safe.
/// </para>
/// </summary>
public sealed class MeshGraph<TNode, TEdge>
{
    private readonly Dictionary<string, TNode> _nodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<GraphEdge<TEdge>>> _adjacency = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, GraphEdge<TEdge>> _edges = new(StringComparer.OrdinalIgnoreCase);

    public int NodeCount => _nodes.Count;
    public int EdgeCount => _edges.Count;

    public IEnumerable<string> NodeIds => _nodes.Keys;
    public IEnumerable<TNode> Nodes => _nodes.Values;
    public IEnumerable<GraphEdge<TEdge>> Edges => _edges.Values;

    public bool ContainsNode(string id) => _nodes.ContainsKey(id);

    public TNode this[string id] => _nodes[id];

    public bool TryGetNode(string id, out TNode node) => _nodes.TryGetValue(id, out node!);

    public void AddNode(string id, TNode node)
    {
        _nodes[id] = node;
        if (!_adjacency.ContainsKey(id))
        {
            _adjacency[id] = new List<GraphEdge<TEdge>>();
        }
    }

    /// <summary>Adds an undirected edge. A second edge between the same pair, or a self-loop, is ignored.</summary>
    public bool AddEdge(string a, string b, double weight, TEdge data)
    {
        if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase) || !_nodes.ContainsKey(a) || !_nodes.ContainsKey(b))
        {
            return false;
        }

        var id = GraphEdge<TEdge>.MakeId(a, b);
        if (_edges.ContainsKey(id))
        {
            return false;
        }

        var edge = new GraphEdge<TEdge>(id, a, b, weight, data);
        _edges[id] = edge;
        _adjacency[a].Add(edge);
        _adjacency[b].Add(edge);
        return true;
    }

    public bool HasEdge(string a, string b) => _edges.ContainsKey(GraphEdge<TEdge>.MakeId(a, b));

    public GraphEdge<TEdge>? FindEdge(string a, string b) =>
        _edges.GetValueOrDefault(GraphEdge<TEdge>.MakeId(a, b));

    /// <summary>The edges touching <paramref name="node"/>: O(1) to reach, O(degree) to walk.</summary>
    public IReadOnlyList<GraphEdge<TEdge>> EdgesOf(string node) =>
        _adjacency.TryGetValue(node, out var list) ? list : Array.Empty<GraphEdge<TEdge>>();

    public int Degree(string node) => EdgesOf(node).Count;

    /// <summary>
    /// Connected components by breadth-first search, considering only nodes and
    /// edges the predicates accept. Each component is listed once.
    /// </summary>
    public List<List<string>> Components(Func<string, bool>? includeNode = null, Func<GraphEdge<TEdge>, bool>? includeEdge = null)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var components = new List<List<string>>();

        foreach (var start in _nodes.Keys)
        {
            if (seen.Contains(start) || (includeNode is not null && !includeNode(start)))
            {
                continue;
            }

            var component = new List<string>();
            var frontier = new Queue<string>();
            frontier.Enqueue(start);
            seen.Add(start);

            while (frontier.Count > 0)
            {
                var node = frontier.Dequeue();
                component.Add(node);

                foreach (var edge in _adjacency[node])
                {
                    if (includeEdge is not null && !includeEdge(edge))
                    {
                        continue;
                    }

                    var next = edge.OtherEnd(node);
                    if ((includeNode is null || includeNode(next)) && seen.Add(next))
                    {
                        frontier.Enqueue(next);
                    }
                }
            }

            components.Add(component);
        }

        return components;
    }

    /* ---------- Traversals [36] ---------- */

    /// <summary>
    /// Breadth-first search from one or more start nodes, entering only nodes
    /// <paramref name="canEnter"/> accepts. A FIFO queue visits the graph in
    /// rings — every node one hop out, then two — so each node's
    /// <see cref="TraversalVisit.Parent"/> is a neighbour on a fewest-hop path
    /// back to a start. O(V + E).
    /// </summary>
    public List<TraversalVisit> BreadthFirst(IEnumerable<string> starts, Func<string, bool> canEnter)
    {
        var visits = new List<TraversalVisit>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var frontier = new Queue<TraversalVisit>();

        foreach (var start in starts.Where(_nodes.ContainsKey))
        {
            if (seen.Add(start))
            {
                frontier.Enqueue(new TraversalVisit(start, 0, 0, null, null));
            }
        }

        while (frontier.Count > 0)
        {
            var visit = frontier.Dequeue() with { Order = visits.Count };
            visits.Add(visit);

            foreach (var edge in OrderedEdgesOf(visit.NodeId))
            {
                var next = edge.OtherEnd(visit.NodeId);
                if (canEnter(next) && seen.Add(next))
                {
                    frontier.Enqueue(new TraversalVisit(next, 0, visit.Depth + 1, visit.NodeId, edge.Id));
                }
            }
        }

        return visits;
    }

    /// <summary>
    /// Depth-first search with an explicit stack (no recursion, so a long chain
    /// of relays cannot overflow the call stack). It follows one branch as far as
    /// it goes before backtracking, so <see cref="TraversalVisit.Depth"/> is the
    /// depth in the DFS tree, not the fewest hops. O(V + E).
    /// </summary>
    public List<TraversalVisit> DepthFirst(IEnumerable<string> starts, Func<string, bool> canEnter)
    {
        var visits = new List<TraversalVisit>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var stack = new Stack<TraversalVisit>();

        // Pushed in reverse so the first start (and the first neighbour) is explored first.
        foreach (var start in starts.Where(_nodes.ContainsKey).Reverse())
        {
            stack.Push(new TraversalVisit(start, 0, 0, null, null));
        }

        while (stack.Count > 0)
        {
            var visit = stack.Pop();
            if (!seen.Add(visit.NodeId))
            {
                continue;
            }

            visits.Add(visit with { Order = visits.Count });

            foreach (var edge in OrderedEdgesOf(visit.NodeId).Reverse())
            {
                var next = edge.OtherEnd(visit.NodeId);
                if (!seen.Contains(next) && canEnter(next))
                {
                    stack.Push(new TraversalVisit(next, 0, visit.Depth + 1, visit.NodeId, edge.Id));
                }
            }
        }

        return visits;
    }

    /// <summary>A node's edges in neighbour-id order, so traversals are deterministic.</summary>
    private IEnumerable<GraphEdge<TEdge>> OrderedEdgesOf(string node) =>
        EdgesOf(node).OrderBy(edge => edge.OtherEnd(node), StringComparer.OrdinalIgnoreCase).ToList();

    public void Clear()
    {
        _nodes.Clear();
        _adjacency.Clear();
        _edges.Clear();
    }
}
