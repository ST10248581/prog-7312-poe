namespace SmartX.Api.Models.Topology;

public enum TraversalAlgorithm
{
    /// <summary>Breadth-first: rings outward from the gateway, so every detour found is a fewest-hop one.</summary>
    Bfs,

    /// <summary>Depth-first: follows one branch to its end before backtracking.</summary>
    Dfs
}

public enum ScanNodeStatus
{
    /// <summary>Reached from the scanned gateway through live nodes.</summary>
    Connected,

    /// <summary>Offline: the traversal never enters it.</summary>
    Down,

    /// <summary>Live, but not reachable from the scanned gateway — it is served by the other one.</summary>
    OtherGateway,

    /// <summary>Live, but every path to any gateway runs through a down node.</summary>
    CutOff
}

/// <summary>
/// The result of a BFS or DFS from a gateway (or every gateway) over the live
/// mesh: which devices in the chosen subnet (zone) are connected, which are
/// down, which are cut off, and where a node whose relay has gone down should
/// hop instead.
/// </summary>
public class ConnectivityScan
{
    /// <summary>The gateway the traversal started from, or "ALL" for every live gateway.</summary>
    public string Root { get; set; } = string.Empty;

    public TraversalAlgorithm Algorithm { get; set; }

    /// <summary>The subnet the results are limited to. Null for the whole mesh.</summary>
    public string? Zone { get; set; }

    /// <summary>False when the scanned gateway is itself down: nothing can be reached.</summary>
    public bool RootOnline { get; set; }

    public int InScope { get; set; }
    public int ConnectedCount { get; set; }
    public int DownCount { get; set; }
    public int OtherGatewayCount { get; set; }
    public int CutOffCount { get; set; }

    /// <summary>Every node the traversal reached across the whole mesh, before the subnet filter.</summary>
    public int TotalVisited { get; set; }

    /// <summary>Furthest depth reached inside the subnet.</summary>
    public int MaxDepth { get; set; }

    /// <summary>The in-scope nodes in the order the traversal reached them, then the unreached ones.</summary>
    public List<ScanNode> Nodes { get; set; } = new();

    /// <summary>Where to hop: live nodes whose designed next hop is down, with the relay the traversal found instead.</summary>
    public List<HopAdvice> HopAdvice { get; set; } = new();

    /// <summary>Traversal-tree links from the root to every reached in-scope node, for highlighting on the graph.</summary>
    public List<string> TraversalLinkIds { get; set; } = new();

    public double TraversalMicroseconds { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime GeneratedUtc { get; set; }
}

public class ScanNode
{
    public string NodeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public MeshNodeKind Kind { get; set; }
    public ScanNodeStatus Status { get; set; }
    public OfflineReason OfflineReason { get; set; }

    /// <summary>Position in the visit order. Null when the traversal never reached it.</summary>
    public int? VisitOrder { get; set; }

    /// <summary>Depth in the traversal tree — fewest hops to the root under BFS.</summary>
    public int? Depth { get; set; }

    /// <summary>The neighbour the traversal reached it from: the node to hop to.</summary>
    public string? HopVia { get; set; }

    /// <summary>For a down node: its live, connected neighbours, which can carry the traffic it used to relay.</summary>
    public List<string> AlternateRelays { get; set; } = new();

    /// <summary>For a down node: how many nodes routed through it on their designed path.</summary>
    public int StrandedDependants { get; set; }

    /// <summary>For a cut-off node: the down neighbours standing between it and a gateway.</summary>
    public List<string> BlockedBy { get; set; } = new();
}

public class HopAdvice
{
    public string NodeId { get; set; } = string.Empty;

    /// <summary>The relay it was designed to forward to, which is now down.</summary>
    public string DownRelay { get; set; } = string.Empty;

    /// <summary>The live neighbour the traversal reached it from. Null when it is cut off.</summary>
    public string? HopVia { get; set; }

    public int? HopsToRoot { get; set; }
    public string Advice { get; set; } = string.Empty;
}
