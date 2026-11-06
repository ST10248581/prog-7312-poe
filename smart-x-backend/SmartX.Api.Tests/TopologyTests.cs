using SmartX.Api.Models.Topology;

namespace SmartX.Api.Tests;

/// <summary>
/// The mesh router: shortest routes to the gateways, the minimum spanning
/// tree, self-healing around failed nodes, route tracing by MAC address and
/// the ordered telemetry history.
/// </summary>
public class TopologyTests
{
    [Fact]
    public void Every_routed_node_reaches_a_gateway_along_active_links()
    {
        var (engine, _, _) = EngineFixture.CreateTopology();
        var topology = engine.GetTopology();
        var nodes = topology.Nodes.ToDictionary(node => node.Id, StringComparer.OrdinalIgnoreCase);
        var links = topology.Links.ToDictionary(link => link.Id, StringComparer.OrdinalIgnoreCase);

        var routed = nodes.Values.Where(node => node.Kind == MeshNodeKind.Sensor && node.Hops.HasValue).ToList();
        Assert.NotEmpty(routed);

        foreach (var node in routed)
        {
            var current = node;
            var hops = 0;
            while (current.NextHop is not null)
            {
                var link = links[string.Compare(current.Id, current.NextHop, StringComparison.OrdinalIgnoreCase) <= 0
                    ? $"{current.Id}|{current.NextHop}"
                    : $"{current.NextHop}|{current.Id}"];

                Assert.True(link.IsActive);
                Assert.True(link.InRoutingTree);
                current = nodes[current.NextHop];
                Assert.NotEqual(MeshNodeState.Offline, current.State);
                hops++;
            }

            Assert.Equal(MeshNodeKind.Gateway, current.Kind);
            Assert.Equal(node.GatewayId, current.Id);
            Assert.Equal(node.Hops, hops);
        }
    }

    [Fact]
    public void Prim_tree_matches_an_independent_Kruskal_build_and_spans_every_routed_node()
    {
        var (engine, _, _) = EngineFixture.CreateTopology();
        var topology = engine.GetTopology();

        var reachable = topology.Nodes.Count(node => node.Kind == MeshNodeKind.Sensor && node.Hops.HasValue);

        // A spanning forest rooted at the gateways has exactly one edge per non-root node.
        Assert.Equal(reachable, topology.Summary.MstEdgeCount);

        // Kruskal over the active links, with the gateways pre-joined into one root.
        var parent = topology.Nodes.ToDictionary(node => node.Id, node => node.Kind == MeshNodeKind.Gateway ? "ROOT" : node.Id, StringComparer.OrdinalIgnoreCase);
        parent["ROOT"] = "ROOT";
        string Find(string id) => parent[id] == id ? id : parent[id] = Find(parent[id]);

        var kruskal = 0d;
        foreach (var link in topology.Links.Where(link => link.IsActive).OrderBy(link => link.Cost))
        {
            var (a, b) = (Find(link.From), Find(link.To));
            if (a != b)
            {
                parent[a] = b;
                kruskal += link.Cost;
            }
        }

        Assert.Equal(Math.Round(kruskal, 3), topology.Summary.MstTotalCost, 3);

        // The shortest-path tree is a spanning tree too, so it can never be cheaper than the MST.
        var routingTreeCost = topology.Links.Where(link => link.InRoutingTree).Sum(link => link.Cost);
        Assert.True(routingTreeCost >= topology.Summary.MstTotalCost - 1e-6);
    }

    [Fact]
    public void Failing_a_relay_heals_its_dependants_onto_routes_that_avoid_it()
    {
        var (engine, _, _) = EngineFixture.CreateTopology();
        var relay = engine.GetTopology().CriticalRelays.First();

        var result = engine.SetSimulatedFailure(relay.NodeId, failed: true)!;

        Assert.True(result.IsSimulatedFailure);
        Assert.Equal(relay.Dependants, result.AffectedNodes);
        Assert.True(result.AffectedNodes > 0);

        // Every dependant either healed onto another path or is reported cut off.
        Assert.True(result.AffectedNodes - result.HealedNodes <= result.IsolatedNodes.Count);

        var failed = result.Topology.Nodes.Single(node => node.Id == relay.NodeId);
        Assert.Equal(MeshNodeState.Offline, failed.State);
        Assert.Equal(OfflineReason.SimulatedOutage, failed.OfflineReason);

        // No live route may pass through the failed node.
        Assert.DoesNotContain(result.Topology.Nodes, node => node.NextHop == relay.NodeId);
        Assert.DoesNotContain(result.Topology.Links, link => link.IsActive && (link.From == relay.NodeId || link.To == relay.NodeId));

        // Bringing it back restores the designed routes.
        var restored = engine.SetSimulatedFailure(relay.NodeId, failed: false)!;
        Assert.False(restored.IsSimulatedFailure);
        Assert.Contains(restored.Topology.Nodes, node => node.NextHop == relay.NodeId);
    }

    [Fact]
    public void The_seeded_mesh_survives_the_loss_of_any_single_sensor_relay()
    {
        var (engine, _, _) = EngineFixture.CreateTopology();
        var relays = engine.GetTopology().Nodes
            .Where(node => node.Kind == MeshNodeKind.Sensor && node.Dependants > 0)
            .Select(node => node.Id)
            .ToList();

        Assert.NotEmpty(relays);

        foreach (var relay in relays)
        {
            var result = engine.SetSimulatedFailure(relay, failed: true)!;
            Assert.Equal(result.AffectedNodes, result.HealedNodes);
            engine.SetSimulatedFailure(relay, failed: false);
        }
    }

    [Fact]
    public void Route_trace_accepts_a_MAC_address_in_any_notation()
    {
        var (engine, _, store) = EngineFixture.CreateTopology();
        var topology = engine.GetTopology();
        var target = topology.Nodes.First(node => node.Kind == MeshNodeKind.Sensor && node.Hops >= 2);
        var mac = store.FindSensorByNodeId(target.Id)!.MacAddress.Replace(':', '-').ToLowerInvariant();

        var trace = engine.TraceRoute(mac);

        Assert.True(trace.Found);
        Assert.Equal("MacAddress", trace.MatchedBy);
        Assert.True(trace.Reachable);
        Assert.Equal(target.Id, trace.Primary!.Hops[0].NodeId);
        Assert.Equal(MeshNodeKind.Gateway, trace.Primary.Hops[^1].Kind);
        Assert.Equal(target.Hops, trace.Primary.HopCount);
        Assert.Equal(trace.Primary.HopCount, trace.Primary.LinkIds.Count);

        // The backup route never uses the primary route's first relay.
        if (trace.Backup is not null)
        {
            Assert.DoesNotContain(trace.Backup.Hops, hop => hop.NodeId == trace.Primary.Hops[1].NodeId);
        }

        Assert.False(engine.TraceRoute("ZZ-999").Found);
    }

    [Fact]
    public void Telemetry_history_is_ordered_paged_and_free_of_duplicates()
    {
        var (engine, _, _) = EngineFixture.CreateTopology();
        var sensor = engine.GetTopology().Nodes.First(node => node.Kind == MeshNodeKind.Sensor);

        var newest = engine.GetTelemetryHistory(sensor.Id, 24, HistorySort.Newest, null, 1, 50)!;
        var oldest = engine.GetTelemetryHistory(sensor.Id, 24, HistorySort.Oldest, null, newest.TotalPages, 50)!;

        Assert.True(newest.TotalCount > 0);
        Assert.Equal(newest.TotalCount, oldest.TotalCount);
        Assert.Equal(newest.TotalCount, newest.Metrics.Sum(metric => metric.Count));

        var times = newest.Items.Select(item => item.TimestampUtc).ToList();
        Assert.Equal(times.OrderDescending(), times);
        Assert.All(newest.Items, item => Assert.InRange(item.TimestampUtc, newest.FromUtc, newest.ToUtc.AddMinutes(5)));
        Assert.Equal(newest.Items.Count, newest.Items.DistinctBy(item => (item.TimestampUtc, item.ReadingType)).Count());

        // Reading the last page oldest-first ends on the newest reading.
        Assert.Equal(newest.Items[0].TimestampUtc, oldest.Items[^1].TimestampUtc);

        // Every reading carries the route it was sent on.
        Assert.All(newest.Items, item => Assert.NotNull(item.RouteHops));

        // Gateways relay telemetry but report none.
        Assert.Null(engine.GetTelemetryHistory("GW-01", 24, HistorySort.Newest, null, 1, 25));
    }

    [Fact]
    public void An_exact_time_range_for_one_metric_reads_only_that_metric_inside_the_range()
    {
        var (engine, _, store) = EngineFixture.CreateTopology();
        var sensor = engine.GetTopology().Nodes.First(node => node.Kind == MeshNodeKind.Sensor);
        var profile = store.FindSensorByNodeId(sensor.Id)!;

        var readings = store.ReadingsFor(profile.Id);
        var metric = readings.GroupBy(reading => reading.ReadingType).OrderByDescending(group => group.Count()).First().Key;

        // A two-hour slice in the middle of the stored history, e.g. 14:00–16:00.
        var middle = readings.Where(reading => reading.ReadingType == metric).OrderBy(reading => reading.TimestampUtc).ElementAt(readings.Count(reading => reading.ReadingType == metric) / 2).TimestampUtc;
        var from = middle.AddHours(-1);
        var to = middle.AddHours(1);

        var history = engine.GetTelemetryHistory(sensor.Id, 24, HistorySort.Oldest, metric, 1, 200, from, to)!;

        var expected = readings
            .Where(reading => reading.ReadingType == metric && reading.TimestampUtc >= from && reading.TimestampUtc <= to)
            .Where(reading => reading.NumericValue.HasValue || reading.BooleanValue.HasValue)
            .Select(reading => reading.TimestampUtc)
            .Distinct()
            .Count();

        Assert.True(history.IsCustomRange);
        Assert.True(expected > 0);
        Assert.Equal(expected, history.TotalCount);
        Assert.All(history.Items, item =>
        {
            Assert.Equal(metric, item.ReadingType);
            Assert.InRange(item.TimestampUtc, from, to);
        });

        // The AVL range search touched a fraction of the tree, not every reading.
        Assert.True(history.TreeHeight > 0);
        Assert.True(history.NodesVisited < history.IndexedReadings, $"{history.NodesVisited} of {history.IndexedReadings}");
    }

    [Fact]
    public void Kruskal_and_Prim_agree_on_every_recompute()
    {
        var (engine, _, _) = EngineFixture.CreateTopology();
        var summary = engine.GetTopology().Summary;
        Assert.True(summary.MstVerified);
        Assert.Equal(summary.MstEdgeCount, summary.KruskalEdgeCount);

        var relay = engine.GetTopology().CriticalRelays.First();
        var failed = engine.SetSimulatedFailure(relay.NodeId, failed: true)!.Topology.Summary;
        Assert.True(failed.MstVerified);
    }

    [Theory]
    [InlineData(TraversalAlgorithm.Bfs)]
    [InlineData(TraversalAlgorithm.Dfs)]
    public void Scanning_from_every_gateway_finds_every_live_routed_node(TraversalAlgorithm algorithm)
    {
        var (engine, _, _) = EngineFixture.CreateTopology();
        var topology = engine.GetTopology();

        var scan = engine.ScanConnectivity("ALL", null, algorithm)!;

        var routed = topology.Nodes.Where(node => node.Kind == MeshNodeKind.Sensor && node.Hops.HasValue).Select(node => node.Id).Order().ToList();
        var connected = scan.Nodes.Where(node => node.Status == ScanNodeStatus.Connected).Select(node => node.NodeId).Order().ToList();

        Assert.True(scan.RootOnline);
        Assert.Equal(routed, connected);
        Assert.Equal(scan.InScope, scan.ConnectedCount + scan.DownCount + scan.CutOffCount + scan.OtherGatewayCount);

        // Visit order is strictly increasing down the list.
        var orders = scan.Nodes.Where(node => node.VisitOrder.HasValue).Select(node => node.VisitOrder!.Value).ToList();
        Assert.Equal(orders.Order(), orders);

        if (algorithm == TraversalAlgorithm.Bfs)
        {
            // BFS depth is the fewest hops, so never more than the routed hop count.
            foreach (var node in scan.Nodes.Where(node => node.Status == ScanNodeStatus.Connected))
            {
                Assert.True(node.Depth <= topology.Nodes.Single(item => item.Id == node.NodeId).Hops);
            }
        }

        Assert.Null(engine.ScanConnectivity("ENV-001", null, algorithm));
    }

    [Fact]
    public void Scanning_after_a_relay_fails_reports_it_down_and_says_where_its_dependants_should_hop()
    {
        var (engine, _, _) = EngineFixture.CreateTopology();
        var relay = engine.GetTopology().CriticalRelays.First();
        var zone = relay.Zone;
        engine.SetSimulatedFailure(relay.NodeId, failed: true);

        var scan = engine.ScanConnectivity("ALL", zone, TraversalAlgorithm.Bfs)!;

        Assert.Equal(zone, scan.Zone);
        Assert.All(scan.Nodes, node => Assert.Equal(zone, node.Zone));

        var down = scan.Nodes.Single(node => node.NodeId == relay.NodeId);
        Assert.Equal(ScanNodeStatus.Down, down.Status);
        Assert.Equal(OfflineReason.SimulatedOutage, down.OfflineReason);
        Assert.Null(down.VisitOrder);
        Assert.True(down.StrandedDependants > 0);

        // Nodes that were designed to relay through it get a hop recommendation that avoids it.
        var whole = engine.ScanConnectivity("ALL", null, TraversalAlgorithm.Bfs)!;
        var advice = whole.HopAdvice.Where(item => item.DownRelay == relay.NodeId).ToList();
        Assert.NotEmpty(advice);
        Assert.All(advice, item =>
        {
            Assert.NotEqual(relay.NodeId, item.HopVia);
            Assert.True(item.HopVia is not null || item.HopsToRoot is null);
        });

        // The traversal tree never passes through the down node.
        Assert.DoesNotContain(scan.TraversalLinkIds, id => id.Split('|').Contains(relay.NodeId));
    }
}
