using SmartX.Api.Data.Collections;

namespace SmartX.Api.Tests;

/// <summary>The custom fixed-capacity collection behind the telemetry windows.</summary>
public class RingBufferTests
{
    [Fact]
    public void Overwrites_the_oldest_item_once_full()
    {
        var buffer = new RingBuffer<int>(3);

        buffer.Add(1);
        buffer.Add(2);
        buffer.Add(3);
        var evictedAny = buffer.Add(4, out var evicted);

        Assert.True(evictedAny);
        Assert.Equal(1, evicted);
        Assert.Equal([2, 3, 4], buffer.ToList());
        Assert.Equal(2, buffer.Oldest);
        Assert.Equal(4, buffer.Latest);
        Assert.Equal(3, buffer.Count);
    }

    [Fact]
    public void TakeLatest_and_NewestFirst_read_the_tail_in_order()
    {
        var buffer = new RingBuffer<int>(4);
        foreach (var value in Enumerable.Range(1, 6))
        {
            buffer.Add(value);
        }

        Assert.Equal([5, 6], buffer.TakeLatest(2));
        Assert.Equal([6, 5, 4, 3], buffer.NewestFirst().ToList());
        Assert.Equal(3, buffer[0]);
    }

    [Fact]
    public void Enumerating_while_adding_throws()
    {
        var buffer = new RingBuffer<int>(2);
        buffer.Add(1);
        buffer.Add(2);

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (var _ in buffer)
            {
                buffer.Add(3);
            }
        });
    }
}
