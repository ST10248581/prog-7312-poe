namespace SmartX.Api.Models.Responses;

/// <summary>
/// Dispatch health for the active filter window — everything the overview strip
/// and the throughput chart need, computed server-side so the browser never has
/// to hold the full command set to describe it.
/// </summary>
public class CommandSummary
{
    public int WindowMinutes { get; set; }

    /// <summary>Commands issued per minute across the window.</summary>
    public double DispatchRate { get; set; }

    public int InFlightCount { get; set; }
    public int QueuedCount { get; set; }
    public int RetryingCount { get; set; }

    /// <summary>Share of finished commands that were acknowledged, as a percentage.</summary>
    public double AcknowledgedRate { get; set; }

    public int FailedCount { get; set; }
    public int ExpiredCount { get; set; }

    public int ManualOverrideCount { get; set; }

    /// <summary>Distinct operators behind the manual overrides.</summary>
    public int OperatorCount { get; set; }

    /// <summary>Median round trip of acknowledged commands, in milliseconds.</summary>
    public int MedianRoundTripMs { get; set; }

    public int TotalCount { get; set; }

    /// <summary>Commands in the slice aimed at a node with an unacknowledged alert.</summary>
    public int AlertingCommandCount { get; set; }

    /// <summary>Distinct nodes behind <see cref="AlertingCommandCount"/>.</summary>
    public int AlertingNodeCount { get; set; }

    /// <summary>
    /// How the slice splits across the operation categories, keyed by category
    /// name. Sent as a map so a new category needs no response-shape change.
    /// </summary>
    public Dictionary<string, int> CategoryCounts { get; set; } = new();

    /// <summary>Commands per bucket across the window, oldest first.</summary>
    public List<int> Throughput { get; set; } = new();

    public DateTime GeneratedUtc { get; set; }
}
