using SmartX.Api.Models.Requests;
using SmartX.Api.Models.Stream;

namespace SmartX.Api.Tests;

/// <summary>
/// The telemetry intake: a FIFO Queue for routine packets, a PriorityQueue for
/// critical ones, a capacity limit on the first, and HashSets that stop repeat
/// alerts.
/// </summary>
public class IntakeQueueTests
{
    [Fact]
    public void Critical_packet_is_processed_before_standard_packets_that_arrived_first()
    {
        var engine = EngineFixture.Create();
        var device = EngineFixture.ReachableDevices(engine).First();
        var readingType = device.Readings[0].ReadingType;

        // Ten routine packets (no value, so always standard), then a lost link.
        var packets = Enumerable.Range(0, 10)
            .Select(_ => new StreamPacketRequest { NodeId = device.NodeId, ReadingType = readingType })
            .Append(new StreamPacketRequest { NodeId = device.NodeId, ReadingType = readingType, LinkUp = false })
            .ToList();

        var before = engine.GetPipelineStatus();
        var result = engine.IngestPackets(packets);
        var after = engine.GetPipelineStatus();

        Assert.Equal(10, result.QueuedStandard);
        Assert.Equal(1, result.ProcessedCritical);
        Assert.Single(result.CriticalAlerts);

        // The critical packet came out while all ten routine packets were still queued.
        Assert.Equal(before.StandardProcessed, after.StandardProcessed);
        Assert.Equal(before.CriticalProcessed + 1, after.CriticalProcessed);
        Assert.Equal(before.StandardQueueDepth + 10, after.StandardQueueDepth);
        Assert.True(result.BypassedStandard >= 10);

        // The next tick drains the routine packets, in their turn.
        engine.RunDispatchCycle();
        Assert.True(engine.GetPipelineStatus().StandardProcessed >= before.StandardProcessed + 10);
    }

    [Fact]
    public void Critical_lane_serves_the_largest_breach_first_even_if_it_arrived_last()
    {
        var engine = EngineFixture.Create();
        var devices = EngineFixture.ReachableDevices(engine);
        var linkLost = devices[0];
        var spiking = devices.First(device => device.NodeId != linkLost.NodeId && device.Readings.Any(reading => !reading.IsBoolean));
        var metric = spiking.Readings.First(reading => !reading.IsBoolean);

        var result = engine.IngestPackets(
        [
            // Arrives first, but a lost link ranks behind a live breach.
            new StreamPacketRequest { NodeId = linkLost.NodeId, ReadingType = linkLost.Readings[0].ReadingType, LinkUp = false },
            new StreamPacketRequest { NodeId = spiking.NodeId, ReadingType = metric.ReadingType, Value = (metric.MaxThreshold ?? 0) + 10_000 }
        ]);

        Assert.Equal(2, result.CriticalAlerts.Count);
        Assert.Equal(spiking.NodeId, result.CriticalAlerts[0].NodeId);
        Assert.Equal(linkLost.NodeId, result.CriticalAlerts[1].NodeId);
    }

    [Fact]
    public void Standard_queue_sheds_oldest_packets_past_its_capacity()
    {
        var engine = EngineFixture.Create();
        var device = EngineFixture.ReachableDevices(engine).First();
        var capacity = engine.GetPipelineStatus().StandardQueueCapacity;

        var flood = Enumerable.Range(0, capacity + 100)
            .Select(_ => new StreamPacketRequest { NodeId = device.NodeId, ReadingType = device.Readings[0].ReadingType })
            .ToList();

        var result = engine.IngestPackets(flood);
        var status = engine.GetPipelineStatus();

        Assert.Equal(capacity, status.StandardQueueDepth);
        Assert.Equal(100, result.DroppedStandard);
        Assert.Equal(100, status.Dropped);
    }

    [Fact]
    public void Repeat_disconnect_is_suppressed_and_counted_against_the_node()
    {
        var engine = EngineFixture.Create();
        var device = EngineFixture.ReachableDevices(engine).First();
        var lost = new StreamPacketRequest { NodeId = device.NodeId, ReadingType = device.Readings[0].ReadingType, LinkUp = false };

        var first = engine.IngestPackets([lost]);
        var second = engine.IngestPackets([lost]);
        var third = engine.IngestPackets([lost]);

        Assert.Single(first.CriticalAlerts);
        Assert.Empty(second.CriticalAlerts);
        Assert.Empty(third.CriticalAlerts);

        var node = engine.GetPipelineStatus().DisconnectedNodes.Single(entry => entry.NodeId == device.NodeId);
        Assert.Equal(2, node.SuppressedCount);
    }

    [Fact]
    public void Set_difference_reports_nodes_newly_disconnected_since_the_last_poll()
    {
        var engine = EngineFixture.Create();
        var known = engine.GetPipelineStatus().DisconnectedNodes.Select(node => node.NodeId).ToList();
        var device = EngineFixture.ReachableDevices(engine).First();

        engine.IngestPackets([new StreamPacketRequest { NodeId = device.NodeId, ReadingType = device.Readings[0].ReadingType, LinkUp = false }]);
        var changes = engine.GetPipelineStatus(known).SetChanges;

        Assert.True(changes.Compared);
        Assert.Equal([device.NodeId], changes.NewlyDisconnected);
        Assert.Empty(changes.Recovered);
        Assert.Equal(known.Count, changes.StillDisconnected);
        Assert.Contains(device.NodeId, changes.NeedsAttention);
    }

    [Fact]
    public void Device_lookup_finds_a_mac_address_in_any_notation()
    {
        var engine = EngineFixture.Create();
        var device = EngineFixture.ReachableDevices(engine).First();
        var otherNotation = device.MacAddress.Replace(':', '-').ToLowerInvariant();

        var lookup = engine.LookupDevice(otherNotation);

        Assert.True(lookup.Found);
        Assert.Equal("MacAddress", lookup.MatchedBy);
        Assert.Equal(device.NodeId, lookup.Device!.NodeId);
        Assert.Equal(device.MacAddress, lookup.NormalisedKey);
    }
}
