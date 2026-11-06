// =============================================================================
// CODE ATTRIBUTION — Kruskal's minimum spanning tree (Part 3)
//
// The union-find structure below is what lets Kruskal's algorithm reject an
// edge that would close a cycle in near-constant time.
//
// Code Attribution [35]
// Author: Kruskal, J.B.
// Year: 1956
// Title: On the shortest spanning subtree of a graph and the traveling salesman problem
// Type: [Source code]
// Available at: <https://doi.org/10.1090/S0002-9939-1956-0078686-7>
// Accessed: [Accessed 6 November 2026]
// Modifications: Kruskal's algorithm takes edges cheapest first and keeps each
//   one that joins two different trees. "Different trees" is answered by this
//   disjoint-set forest with union by rank and path compression (path halving),
//   over string node ids.
// Reference: Kruskal, J.B., 1956. On the shortest spanning subtree of a graph and the traveling salesman problem [Source code] Available at: <https://doi.org/10.1090/S0002-9939-1956-0078686-7> [Accessed 6 November 2026].
// =============================================================================

namespace SmartX.Api.Data.Collections;

/// <summary>
/// A disjoint-set (union-find) forest. Each set is a tree whose root names it;
/// <see cref="Find"/> walks to the root and flattens the path as it goes, and
/// <see cref="Union"/> hangs the shorter tree under the taller one. Both run in
/// near-constant amortised time. Not thread-safe.
/// </summary>
public sealed class DisjointSet
{
    private readonly Dictionary<string, string> _parent = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _rank = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The root of the set holding <paramref name="item"/>. An unseen item starts as a set of its own.</summary>
    public string Find(string item)
    {
        if (!_parent.ContainsKey(item))
        {
            _parent[item] = item;
            _rank[item] = 0;
            return item;
        }

        while (!string.Equals(_parent[item], item, StringComparison.OrdinalIgnoreCase))
        {
            // Path halving: point every other node at its grandparent.
            _parent[item] = _parent[_parent[item]];
            item = _parent[item];
        }

        return item;
    }

    /// <summary>Merges the two sets. Returns false when they were already one set — the edge would close a cycle.</summary>
    public bool Union(string a, string b)
    {
        var rootA = Find(a);
        var rootB = Find(b);
        if (string.Equals(rootA, rootB, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (_rank[rootA] < _rank[rootB])
        {
            (rootA, rootB) = (rootB, rootA);
        }

        _parent[rootB] = rootA;
        if (_rank[rootA] == _rank[rootB])
        {
            _rank[rootA]++;
        }

        return true;
    }

    public bool Connected(string a, string b) =>
        string.Equals(Find(a), Find(b), StringComparison.OrdinalIgnoreCase);
}
