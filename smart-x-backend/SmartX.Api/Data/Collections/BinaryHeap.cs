// =============================================================================
// CODE ATTRIBUTION — Heaps (Part 3)
//
// The array-backed binary min-heap below — the sift-up / sift-down operations
// and the parent/child index arithmetic — was written with reference to the
// sources below.
//
// Code Attribution [26]
// Author: Cormen, T.H., Leiserson, C.E., Rivest, R.L. and Stein, C.
// Year: 2022
// Title: Introduction to Algorithms, Chapter 6: Heapsort (Heaps and priority queues)
// Version: 4th edition
// Type: [Source code]
// Available at: <https://mitpress.mit.edu/9780262046305/introduction-to-algorithms/>
// Accessed: [Accessed 2 November 2026]
// Modifications: Adapted the MIN-HEAPIFY, HEAP-EXTRACT-MIN and MIN-HEAP-INSERT
//   pseudocode to a zero-based generic BinaryHeap<TElement,TPriority> with an
//   IComparer<TPriority>, a growable backing array and an insertion counter so
//   equal priorities come out in insertion order.
// Reference: Cormen, T.H., Leiserson, C.E., Rivest, R.L. and Stein, C., 2022. Introduction to Algorithms, Chapter 6: Heapsort (4th edition) [Source code] Available at: <https://mitpress.mit.edu/9780262046305/introduction-to-algorithms/> [Accessed 2 November 2026].
//
// Code Attribution [15]
// Author: Microsoft
// Year: 2025
// Title: PriorityQueue<TElement,TPriority> Class
// Type: [Source code]
// Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.priorityqueue-2>
// Accessed: [Accessed 2 November 2026]
// Modifications: No code copied. The Enqueue / Dequeue / TryPeek surface and
//   the separate element and priority type parameters follow the BCL type, so
//   the routing code reads the same as the Part 2 critical lane.
// Reference: Microsoft, 2025. PriorityQueue<TElement,TPriority> Class [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.priorityqueue-2> [Accessed 2 November 2026].
// =============================================================================

namespace SmartX.Api.Data.Collections;

/// <summary>
/// A hand-written binary min-heap: the priority queue behind the mesh router's
/// Dijkstra search and the Prim minimum-spanning-tree build.
/// <para>
/// The heap is a complete binary tree stored level by level in an array, so the
/// parent of slot <c>i</c> is <c>(i - 1) / 2</c> and its children are
/// <c>2i + 1</c> and <c>2i + 2</c>. Every parent is no larger than its children,
/// which keeps the minimum at slot 0.
/// </para>
/// <list type="bullet">
///   <item><description><see cref="Enqueue"/> appends at the end and sifts up: O(log n).</description></item>
///   <item><description><see cref="Dequeue"/> moves the last item to the root and sifts down: O(log n).</description></item>
///   <item><description><see cref="TryPeek"/> reads the root: O(1).</description></item>
/// </list>
/// Equal priorities leave in the order they were added (an insertion counter
/// breaks ties), so routing is deterministic. Not thread-safe.
/// </summary>
public sealed class BinaryHeap<TElement, TPriority>
{
    private (TElement Element, TPriority Priority, long Order)[] _items;
    private int _count;
    private long _inserted;
    private readonly IComparer<TPriority> _comparer;

    public BinaryHeap(int initialCapacity = 16, IComparer<TPriority>? comparer = null)
    {
        _items = new (TElement, TPriority, long)[Math.Max(initialCapacity, 1)];
        _comparer = comparer ?? Comparer<TPriority>.Default;
    }

    public int Count => _count;

    public bool IsEmpty => _count == 0;

    /// <summary>Adds an item and restores the heap order by sifting it up.</summary>
    public void Enqueue(TElement element, TPriority priority)
    {
        if (_count == _items.Length)
        {
            Array.Resize(ref _items, _items.Length * 2);
        }

        _items[_count] = (element, priority, _inserted++);
        SiftUp(_count);
        _count++;
    }

    /// <summary>Removes and returns the item with the smallest priority.</summary>
    public TElement Dequeue()
    {
        if (!TryDequeue(out var element, out _))
        {
            throw new InvalidOperationException("The heap is empty.");
        }

        return element;
    }

    public bool TryDequeue(out TElement element, out TPriority priority)
    {
        if (_count == 0)
        {
            element = default!;
            priority = default!;
            return false;
        }

        (element, priority, _) = _items[0];

        _count--;
        if (_count > 0)
        {
            // The last leaf takes the root's place, then sinks to where it belongs.
            _items[0] = _items[_count];
            SiftDown(0);
        }

        // Release the reference held by the vacated slot.
        _items[_count] = default;
        return true;
    }

    public bool TryPeek(out TElement element, out TPriority priority)
    {
        if (_count == 0)
        {
            element = default!;
            priority = default!;
            return false;
        }

        (element, priority, _) = _items[0];
        return true;
    }

    public void Clear()
    {
        Array.Clear(_items, 0, _count);
        _count = 0;
    }

    // HEAP-INSERT's "while parent is larger, swap" loop [26].
    private void SiftUp(int index)
    {
        var item = _items[index];

        while (index > 0)
        {
            var parent = (index - 1) / 2;
            if (!Less(item, _items[parent]))
            {
                break;
            }

            _items[index] = _items[parent];
            index = parent;
        }

        _items[index] = item;
    }

    // MIN-HEAPIFY [26]: swap with the smaller child until neither child is smaller.
    private void SiftDown(int index)
    {
        var item = _items[index];

        while (true)
        {
            var left = 2 * index + 1;
            if (left >= _count)
            {
                break;
            }

            var right = left + 1;
            var smaller = right < _count && Less(_items[right], _items[left]) ? right : left;

            if (!Less(_items[smaller], item))
            {
                break;
            }

            _items[index] = _items[smaller];
            index = smaller;
        }

        _items[index] = item;
    }

    private bool Less((TElement Element, TPriority Priority, long Order) left, (TElement Element, TPriority Priority, long Order) right)
    {
        var compared = _comparer.Compare(left.Priority, right.Priority);
        return compared != 0 ? compared < 0 : left.Order < right.Order;
    }
}
