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
// =============================================================================

namespace SmartX.Api.Data.Collections;

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

    public void Clear()
    {
        _nodes.Clear();
        _adjacency.Clear();
        _edges.Clear();
    }
}
