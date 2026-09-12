namespace SmartX.Api.Models.Telemetry;

/// <summary>
/// Tiers of the deployment hierarchy, ordered from the outside in. A child must
/// always sit exactly one tier below its parent: Facility -> Zone -> Sub-Zone -> Node.
/// </summary>
public enum DeploymentTier
{
    Facility = 0,
    Zone = 1,
    SubZone = 2,
    Node = 3
}

/// <summary>
/// One level of the nested deployment tree. The shape is recursive — a node holds
/// a list of nodes — which is why it is walked recursively rather than with a
/// fixed set of loops.
/// </summary>
public class DeploymentNode
{
    public string Name { get; set; } = string.Empty;
    public DeploymentTier Tier { get; set; }

    /// <summary>Set only on <see cref="DeploymentTier.Node"/> leaves.</summary>
    public Guid? SensorProfileId { get; set; }

    public SensorStatus? Status { get; set; }
    public bool IsActive { get; set; } = true;

    public List<DeploymentNode> Children { get; set; } = new();
}

public enum DeploymentIssueKind
{
    UnnamedNode,
    TierOutOfOrder,
    EmptyBranch,
    DuplicateSibling,
    OrphanedSensor,
    DepthExceeded,
    UnreachableNode,
    CircularReference
}

/// <summary>A single problem found while walking the tree, with the path that reached it.</summary>
public class DeploymentIssue
{
    public string Path { get; set; } = string.Empty;
    public DeploymentTier Tier { get; set; }
    public DeploymentIssueKind Kind { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>Outcome of a full recursive validation pass over a deployment tree.</summary>
public class DeploymentValidationReport
{
    public DeploymentNode? Root { get; set; }

    public int NodesVisited { get; set; }
    public int MaxDepthReached { get; set; }
    public int SensorsPlaced { get; set; }

    /// <summary>Fully qualified paths of every safely configured sensor node.</summary>
    public List<string> ValidPaths { get; set; } = new();

    public List<DeploymentIssue> Issues { get; set; } = new();

    /// <summary>True when nothing critical was found — warnings alone do not fail a deployment.</summary>
    public bool IsValid => Issues.All(issue => issue.Severity != AlertSeverity.Critical);

    public DateTime GeneratedUtc { get; set; }
}
