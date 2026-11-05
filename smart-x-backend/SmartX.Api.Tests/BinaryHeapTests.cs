using SmartX.Api.Data.Collections;

namespace SmartX.Api.Tests;

/// <summary>The hand-written min-heap behind Dijkstra and Prim.</summary>
public class BinaryHeapTests
{
    [Fact]
    public void Dequeues_in_ascending_priority_order()
    {
        var random = new Random(42);
        var values = Enumerable.Range(0, 500).Select(_ => random.Next(0, 1_000)).ToList();
        var heap = new BinaryHeap<int, int>(initialCapacity: 2);

        foreach (var value in values)
        {
            heap.Enqueue(value, value);
        }

        var drained = new List<int>();
        while (heap.TryDequeue(out var value, out _))
        {
            drained.Add(value);
        }

        Assert.Equal(values.Order(), drained);
        Assert.True(heap.IsEmpty);
    }

    [Fact]
    public void Equal_priorities_leave_in_insertion_order()
    {
        var heap = new BinaryHeap<string, double>();
        heap.Enqueue("first", 1);
        heap.Enqueue("cheap", 0.5);
        heap.Enqueue("second", 1);
        heap.Enqueue("third", 1);

        Assert.True(heap.TryPeek(out var top, out _));
        Assert.Equal("cheap", top);
        Assert.Equal(["cheap", "first", "second", "third"], new[] { heap.Dequeue(), heap.Dequeue(), heap.Dequeue(), heap.Dequeue() });
        Assert.Throws<InvalidOperationException>(() => heap.Dequeue());
    }
}
