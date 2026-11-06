// =============================================================================
// CODE ATTRIBUTION — Binary search trees and AVL trees (Part 3)
//
// The self-balancing binary search tree below — the height bookkeeping, the
// four rotation cases and the pruned in-order range search — was written with
// reference to the sources below.
//
// Code Attribution [33]
// Author: Adelson-Velsky, G.M. and Landis, E.M.
// Year: 1962
// Title: An algorithm for the organization of information
// Type: [Source code]
// Available at: <https://zhjwpku.com/assets/pdf/AED2-10-avl-paper.pdf>
// Accessed: [Accessed 6 November 2026]
// Modifications: Implemented the AVL balance rule (the heights of a node's two
//   subtrees differ by at most one) with a stored height per node and the
//   left-left, right-right, left-right and right-left rotation cases, as a
//   generic AvlTree<TKey,TValue> with an IComparer<TKey>. A duplicate key is
//   refused rather than overwritten.
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
// Modifications: Adapted the BST keys(lo, hi) range search — recurse left only
//   when lo is below the node, right only when hi is above it — to collect
//   values into a list and count the nodes it touched, so the API can show how
//   little of the tree a time-window query reads. Min/Max follow the same API.
// Reference: Sedgewick, R. and Wayne, K., 2011. Algorithms, Section 3.2: Binary Search Trees (4th edition) [Source code] Available at: <https://algs4.cs.princeton.edu/32bst/> [Accessed 6 November 2026].
//
// Code Attribution [31]
// Author: Microsoft
// Year: 2025
// Title: SortedSet<T> Class (System.Collections.Generic)
// Type: [Source code]
// Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.sortedset-1>
// Accessed: [Accessed 2 November 2026]
// Modifications: No code copied. The inclusive Range(low, high) query mirrors
//   SortedSet.GetViewBetween, and Add returning false on a duplicate mirrors
//   SortedSet.Add.
// Reference: Microsoft, 2025. SortedSet<T> Class (System.Collections.Generic) [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.sortedset-1> [Accessed 2 November 2026].
// =============================================================================

namespace SmartX.Api.Data.Collections;

/// <summary>
/// A hand-written AVL tree: a binary search tree that rebalances itself after
/// every insert, so its height stays within about 1.44·log₂(n) whatever order
/// the keys arrive in. Telemetry arrives in time order — the worst case for a
/// plain BST, which would degrade into a linked list — so the balancing is what
/// keeps a time-window query logarithmic.
/// <list type="bullet">
///   <item><description><see cref="Add"/>: O(log n), at most two rotations.</description></item>
///   <item><description><see cref="TryGetValue"/>, <see cref="Min"/>, <see cref="Max"/>: O(log n).</description></item>
///   <item><description><see cref="Range"/>: O(log n + k) for k results — only the subtrees that can hold keys in the range are entered.</description></item>
/// </list>
/// Not thread-safe.
/// </summary>
public sealed class AvlTree<TKey, TValue>
{
    private sealed class Node
    {
        public Node(TKey key, TValue value)
        {
            Key = key;
            Value = value;
            Height = 1;
        }

        public TKey Key { get; }
        public TValue Value { get; }
        public Node? Left { get; set; }
        public Node? Right { get; set; }

        /// <summary>Nodes on the longest path down to a leaf, counting this one.</summary>
        public int Height { get; set; }
    }

    private readonly IComparer<TKey> _comparer;
    private Node? _root;

    public AvlTree(IComparer<TKey>? comparer = null)
    {
        _comparer = comparer ?? Comparer<TKey>.Default;
    }

    public int Count { get; private set; }

    /// <summary>The tree's height: 0 when empty. Stays O(log n) because of the rebalancing.</summary>
    public int Height => HeightOf(_root);

    /* ---------- Insert ---------- */

    /// <summary>Inserts the key. Returns false, leaving the tree unchanged, when the key is already present.</summary>
    public bool Add(TKey key, TValue value)
    {
        var added = false;
        _root = Insert(_root, key, value, ref added);
        if (added)
        {
            Count++;
        }

        return added;
    }

    private Node Insert(Node? node, TKey key, TValue value, ref bool added)
    {
        if (node is null)
        {
            added = true;
            return new Node(key, value);
        }

        var compare = _comparer.Compare(key, node.Key);
        if (compare < 0)
        {
            node.Left = Insert(node.Left, key, value, ref added);
        }
        else if (compare > 0)
        {
            node.Right = Insert(node.Right, key, value, ref added);
        }
        else
        {
            return node;
        }

        return added ? Rebalance(node) : node;
    }

    /* ---------- Balancing ---------- */

    private static int HeightOf(Node? node) => node?.Height ?? 0;

    /// <summary>Left height minus right height. AVL keeps it in [-1, 1] at every node.</summary>
    private static int BalanceOf(Node node) => HeightOf(node.Left) - HeightOf(node.Right);

    private static void Update(Node node) => node.Height = 1 + Math.Max(HeightOf(node.Left), HeightOf(node.Right));

    private static Node Rebalance(Node node)
    {
        Update(node);
        var balance = BalanceOf(node);

        if (balance > 1)
        {
            // Left-right case: straighten the left child first.
            if (BalanceOf(node.Left!) < 0)
            {
                node.Left = RotateLeft(node.Left!);
            }

            return RotateRight(node);
        }

        if (balance < -1)
        {
            // Right-left case.
            if (BalanceOf(node.Right!) > 0)
            {
                node.Right = RotateRight(node.Right!);
            }

            return RotateLeft(node);
        }

        return node;
    }

    /// <summary>
    ///       node            pivot
    ///      /    \          /     \
    ///   pivot    C   →    A      node
    ///   /   \                   /    \
    ///  A     B                 B      C
    /// </summary>
    private static Node RotateRight(Node node)
    {
        var pivot = node.Left!;
        node.Left = pivot.Right;
        pivot.Right = node;
        Update(node);
        Update(pivot);
        return pivot;
    }

    private static Node RotateLeft(Node node)
    {
        var pivot = node.Right!;
        node.Right = pivot.Left;
        pivot.Left = node;
        Update(node);
        Update(pivot);
        return pivot;
    }

    /* ---------- Lookups ---------- */

    public bool TryGetValue(TKey key, out TValue value)
    {
        var node = _root;
        while (node is not null)
        {
            var compare = _comparer.Compare(key, node.Key);
            if (compare == 0)
            {
                value = node.Value;
                return true;
            }

            node = compare < 0 ? node.Left : node.Right;
        }

        value = default!;
        return false;
    }

    public bool TryGetMin(out TKey key)
    {
        var node = _root;
        while (node?.Left is not null) node = node.Left;
        key = node is null ? default! : node.Key;
        return node is not null;
    }

    public bool TryGetMax(out TKey key)
    {
        var node = _root;
        while (node?.Right is not null) node = node.Right;
        key = node is null ? default! : node.Key;
        return node is not null;
    }

    /* ---------- Range search ---------- */

    /// <summary>
    /// Every value whose key lies in [<paramref name="low"/>, <paramref name="high"/>],
    /// in key order. A subtree is entered only if it can hold a key in range, so
    /// the work is the two root-to-leaf boundary paths plus the k results — not
    /// a scan of the whole tree. <paramref name="nodesVisited"/> reports how many
    /// nodes were touched.
    /// </summary>
    public List<TValue> Range(TKey low, TKey high, out int nodesVisited)
    {
        var results = new List<TValue>();
        var visited = 0;
        if (_comparer.Compare(low, high) <= 0)
        {
            Collect(_root, low, high, results, ref visited);
        }

        nodesVisited = visited;
        return results;
    }

    private void Collect(Node? node, TKey low, TKey high, List<TValue> results, ref int visited)
    {
        if (node is null)
        {
            return;
        }

        visited++;
        var aboveLow = _comparer.Compare(low, node.Key) < 0;
        var belowHigh = _comparer.Compare(high, node.Key) > 0;

        // Keys smaller than this one can only be in range if low is below it.
        if (aboveLow)
        {
            Collect(node.Left, low, high, results, ref visited);
        }

        if (_comparer.Compare(low, node.Key) <= 0 && _comparer.Compare(high, node.Key) >= 0)
        {
            results.Add(node.Value);
        }

        if (belowHigh)
        {
            Collect(node.Right, low, high, results, ref visited);
        }
    }

    /// <summary>Every value in key order (an in-order walk).</summary>
    public IEnumerable<TValue> InOrder()
    {
        var stack = new Stack<Node>();
        var node = _root;
        while (node is not null || stack.Count > 0)
        {
            while (node is not null)
            {
                stack.Push(node);
                node = node.Left;
            }

            node = stack.Pop();
            yield return node.Value;
            node = node.Right;
        }
    }

    /// <summary>
    /// Checks the BST ordering and the AVL balance rule at every node. Used by
    /// the tests; O(n).
    /// </summary>
    public bool IsValid()
    {
        return Check(_root, null, null, out _);

        // Every key must sit strictly between the bounds its ancestors set.
        bool Check(Node? node, Node? lowerBound, Node? upperBound, out int height)
        {
            height = 0;
            if (node is null) return true;

            if (lowerBound is not null && _comparer.Compare(node.Key, lowerBound.Key) <= 0) return false;
            if (upperBound is not null && _comparer.Compare(node.Key, upperBound.Key) >= 0) return false;
            if (!Check(node.Left, lowerBound, node, out var left) || !Check(node.Right, node, upperBound, out var right)) return false;
            if (Math.Abs(left - right) > 1) return false;

            height = 1 + Math.Max(left, right);
            return height == node.Height;
        }
    }
}
