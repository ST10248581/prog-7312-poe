using SmartX.Api.Data.Collections;

namespace SmartX.Api.Tests;

/// <summary>The hand-written AVL tree behind the telemetry history.</summary>
public class AvlTreeTests
{
    [Fact]
    public void Inserting_in_time_order_stays_balanced()
    {
        // Sorted input is the worst case for an unbalanced BST: it would be 10 000 tall.
        var tree = new AvlTree<DateTime, int>();
        var start = new DateTime(2026, 11, 6, 0, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < 10_000; i++)
        {
            Assert.True(tree.Add(start.AddSeconds(30 * i), i));
        }

        Assert.Equal(10_000, tree.Count);
        Assert.True(tree.IsValid());

        // AVL guarantees height < 1.45 log2(n + 2).
        Assert.True(tree.Height <= (int)Math.Ceiling(1.45 * Math.Log2(10_002)), $"height {tree.Height}");
    }

    [Fact]
    public void Range_returns_exactly_the_keys_in_the_window_in_order_without_scanning_the_tree()
    {
        var tree = new AvlTree<DateTime, DateTime>();
        var day = new DateTime(2026, 11, 6, 0, 0, 0, DateTimeKind.Utc);
        var random = new Random(7);
        var keys = Enumerable.Range(0, 2_880).Select(i => day.AddSeconds(i * 30)).OrderBy(_ => random.Next()).ToList();
        keys.ForEach(key => tree.Add(key, key));
        Assert.True(tree.IsValid());

        // "Between 14:00 and 16:00"
        var from = day.AddHours(14);
        var to = day.AddHours(16);
        var result = tree.Range(from, to, out var visited);

        var expected = keys.Where(key => key >= from && key <= to).Order().ToList();
        Assert.Equal(expected, result);
        Assert.Equal(241, result.Count);

        // Only the boundary paths and the results are touched, not all 2 880 nodes.
        Assert.True(visited < result.Count + 4 * tree.Height, $"visited {visited}");

        Assert.Empty(tree.Range(to, from, out _));
    }

    [Fact]
    public void Duplicates_are_refused_and_lookups_work()
    {
        var tree = new AvlTree<int, string>();
        foreach (var key in new[] { 50, 20, 70, 10, 30, 60, 80, 25, 35, 33 })
        {
            Assert.True(tree.Add(key, $"v{key}"));
        }

        Assert.False(tree.Add(30, "again"));
        Assert.Equal(10, tree.Count);
        Assert.True(tree.IsValid());

        Assert.True(tree.TryGetValue(33, out var value));
        Assert.Equal("v33", value);
        Assert.False(tree.TryGetValue(34, out _));

        Assert.True(tree.TryGetMin(out var min));
        Assert.True(tree.TryGetMax(out var max));
        Assert.Equal(10, min);
        Assert.Equal(80, max);
        Assert.Equal(tree.InOrder().ToList(), tree.InOrder().Order().ToList());
    }
}
