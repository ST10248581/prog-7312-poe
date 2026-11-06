namespace SmartX.Api.Models.Topology;

public enum MeshNodeKind
{
    /// <summary>A border router: the root every route ends at.</summary>
    Gateway,
    Sensor
}

public enum MeshNodeState
{
    Online,

    /// <summary>Reachable, but the sensor is in a warning state, so routes avoid relaying through it.</summary>
    Degraded,

    /// <summary>Off the mesh: its links carry nothing and no route may pass through it.</summary>
    Offline,

    /// <summary>Up, but every path to a gateway runs through an offline node.</summary>
    Isolated
}

public enum OfflineReason
{
    None,

    /// <summary>The gateway reported the link lost (the Part 2 disconnected-node set).</summary>
    LinkLost,

    /// <summary>The sensor profile is deactivated or registered offline.</summary>
    Deactivated,

    /// <summary>Taken down from this page to demonstrate self-healing.</summary>
    SimulatedOutage
}

public enum MeshLinkKind
{
    /// <summary>Sensor-to-sensor radio hop.</summary>
    Radio,

    /// <summary>Radio link that terminates at a gateway.</summary>
    GatewayUplink
}

/// <summary>
/// The whole mesh as the dashboard draws it: every node with its position and
/// current route, every link with its weight, and which links make up the
/// minimum spanning tree and the live routing tree.
/// </summary>
public class MeshTopology
{
    public List<MeshNode> Nodes { get; set; } = new();
    public List<MeshLink> Links { get; set; } = new();
    public TopologySummary Summary { get; set; } = new();

    /// <summary>Relays the most nodes depend on, worst first — the mesh's single points of failure.</summary>
    public List<CriticalRelay> CriticalRelays { get; set; } = new();

    public List<string> Zones { get; set; } = new();
    public DateTime GeneratedUtc { get; set; }
}

public class MeshNode
{
    /// <summary>The node id (e.g. "ENV-001", or "GW-01" for a gateway).</summary>
    public string Id { get; set; } = string.Empty;

    public Guid? SensorProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public MeshNodeKind Kind { get; set; }
    public SensorCategory? Category { get; set; }
    public string Zone { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;

    /// <summary>Layout position in a 0–100 square, stable between requests.</summary>
    public double X { get; set; }
    public double Y { get; set; }

    public MeshNodeState State { get; set; }
    public OfflineReason OfflineReason { get; set; }
    public bool IsSimulatedFailure { get; set; }

    /// <summary>Radio links touching the node, active or not.</summary>
    public int Degree { get; set; }

    /* ---------- Current route (null when offline or isolated) ---------- */

    public int? Hops { get; set; }
    public double? PathCost { get; set; }
    public string? GatewayId { get; set; }

    /// <summary>The neighbour this node forwards to — its parent in the routing tree.</summary>
    public string? NextHop { get; set; }

    /// <summary>Nodes whose route passes through this one: its subtree size in the routing tree.</summary>
    public int Dependants { get; set; }

    /// <summary>True when the mesh has healed around a failure and the node uses a different path from its designed one.</summary>
    public bool IsRerouted { get; set; }
}

public class MeshLink
{
    /// <summary>The two endpoint ids in ordinal order, joined by "|".</summary>
    public string Id { get; set; } = string.Empty;

    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public MeshLinkKind Kind { get; set; }

    public double DistanceMetres { get; set; }
    public double RssiDbm { get; set; }

    /// <summary>Share of frames delivered first time (0–1).</summary>
    public double DeliveryRatio { get; set; }

    /// <summary>Expected transmissions per delivered frame (1 / delivery ratio): the edge weight.</summary>
    public double Cost { get; set; }

    public double LatencyMs { get; set; }

    /// <summary>Both endpoints are up, so the link can carry traffic.</summary>
    public bool IsActive { get; set; }

    /// <summary>Part of the minimum spanning tree over the live mesh.</summary>
    public bool InMst { get; set; }

    /// <summary>Used by at least one current shortest route to a gateway.</summary>
    public bool InRoutingTree { get; set; }
}

public class TopologySummary
{
    public int SensorNodes { get; set; }
    public int ActiveNodes { get; set; }
    public int DegradedNodes { get; set; }
    public int OfflineNodes { get; set; }
    public int IsolatedNodes { get; set; }
    public int ReroutedNodes { get; set; }

    public int Gateways { get; set; }
    public int OnlineGateways { get; set; }

    public int TotalLinks { get; set; }
    public int ActiveLinks { get; set; }

    public double AverageHops { get; set; }
    public int MaxHops { get; set; }

    /// <summary>Edges in the minimum spanning tree (one fewer than the nodes it spans, per gateway).</summary>
    public int MstEdgeCount { get; set; }

    /// <summary>Sum of the MST's edge weights: the cheapest cost of keeping every live node connected.</summary>
    public double MstTotalCost { get; set; }

    /// <summary>Sum of every active link's weight, to compare the MST against.</summary>
    public double ActiveMeshCost { get; set; }

    /// <summary>Active links the MST does not need: the mesh's spare paths for self-healing.</summary>
    public int RedundantLinks { get; set; }

    /* ---------- Compute cost ---------- */

    public double DijkstraMicroseconds { get; set; }
    public double PrimMicroseconds { get; set; }

    /* ---------- Kruskal cross-check ---------- */

    /// <summary>Total weight of the spanning forest built independently by Kruskal's algorithm.</summary>
    public double KruskalTotalCost { get; set; }

    public int KruskalEdgeCount { get; set; }
    public double KruskalMicroseconds { get; set; }

    /// <summary>True when Prim and Kruskal agree on the edge count and the total cost.</summary>
    public bool MstVerified { get; set; }

    /// <summary>Bumped every time a recompute changes at least one route.</summary>
    public int RouteVersion { get; set; }

    public DateTime LastRecomputedUtc { get; set; }
}

public class CriticalRelay
{
    public string NodeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;

    /// <summary>Nodes that lose their current route if this relay fails.</summary>
    public int Dependants { get; set; }
}
