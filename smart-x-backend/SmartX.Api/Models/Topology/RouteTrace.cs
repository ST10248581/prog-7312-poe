namespace SmartX.Api.Models.Topology;

/// <summary>
/// A node's path back to the gateway, traced by its MAC address or node id:
/// the current shortest route, the route it was designed to use, a backup that
/// avoids its first relay, and its path along the minimum spanning tree.
/// </summary>
public class RouteTrace
{
    public string Key { get; set; } = string.Empty;

    /// <summary>The key as it was probed: node ids as typed, MAC addresses in canonical form.</summary>
    public string NormalisedKey { get; set; } = string.Empty;

    public bool Found { get; set; }

    /// <summary>"NodeId" or "MacAddress": which index answered. Null when neither did.</summary>
    public string? MatchedBy { get; set; }

    public MeshNode? Node { get; set; }

    /// <summary>False when the node is offline or isolated: there is no route to show.</summary>
    public bool Reachable { get; set; }

    /// <summary>Plain-language outcome, shown under the hop list.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Source first, gateway last.</summary>
    public RoutePath? Primary { get; set; }

    /// <summary>The path the node uses when every node is up — what it would be on if nothing had failed.</summary>
    public RoutePath? Designed { get; set; }

    /// <summary>The best path that avoids the primary route's first relay: where traffic goes if that relay fails next.</summary>
    public RoutePath? Backup { get; set; }

    /// <summary>The node's path to its gateway along the minimum spanning tree.</summary>
    public RoutePath? Spanning { get; set; }

    public bool IsRerouted { get; set; }

    /// <summary>Time taken by the search that produced the backup route.</summary>
    public double BackupSearchMicroseconds { get; set; }

    public DateTime GeneratedUtc { get; set; }
}

public class RoutePath
{
    public List<RouteHop> Hops { get; set; } = new();

    /// <summary>Links traversed — one fewer than the nodes on the path.</summary>
    public int HopCount { get; set; }

    public double TotalCost { get; set; }
    public double TotalLatencyMs { get; set; }
    public string GatewayId { get; set; } = string.Empty;

    /// <summary>Link ids in path order, for highlighting on the graph.</summary>
    public List<string> LinkIds { get; set; } = new();
}

public class RouteHop
{
    public int Index { get; set; }
    public string NodeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public MeshNodeKind Kind { get; set; }
    public MeshNodeState State { get; set; }

    /// <summary>Cost of the link from this node to the next one. Null on the gateway.</summary>
    public double? LinkCost { get; set; }

    public double? LinkLatencyMs { get; set; }
    public double? LinkRssiDbm { get; set; }

    /// <summary>Cost accumulated from the source up to this node.</summary>
    public double CumulativeCost { get; set; }
}

/// <summary>What taking a node down or bringing it back did to the mesh.</summary>
public class NodeFailureResult
{
    public string NodeId { get; set; } = string.Empty;
    public bool IsSimulatedFailure { get; set; }

    /// <summary>Nodes that routed through the node before the change.</summary>
    public int AffectedNodes { get; set; }

    /// <summary>Of those, how many found another path to a gateway.</summary>
    public int HealedNodes { get; set; }

    /// <summary>Nodes left with no path at all.</summary>
    public List<string> IsolatedNodes { get; set; } = new();

    /// <summary>Every node whose route changed, either way.</summary>
    public List<string> ChangedRoutes { get; set; } = new();

    public string Message { get; set; } = string.Empty;
    public MeshTopology Topology { get; set; } = new();
}
