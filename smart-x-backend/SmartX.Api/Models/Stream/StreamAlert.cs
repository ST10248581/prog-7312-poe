namespace SmartX.Api.Models.Stream;

/// <summary>
/// An alert raised by the command engine's intake while processing a packet —
/// a new threshold breach, an escalation, or a node dropping off the mesh.
/// Duplicates never become one of these: the engine's error-state set stops
/// them first.
/// </summary>
public class StreamAlert
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string NodeId { get; init; } = string.Empty;
    public string SensorName { get; init; } = string.Empty;
    public string Zone { get; init; } = string.Empty;

    public AlertType AlertType { get; init; }
    public ReadingType? ReadingType { get; init; }
    public double? Value { get; init; }
    public double? Limit { get; init; }
    public string Unit { get; init; } = string.Empty;
    public AlertSeverity Severity { get; init; }

    /// <summary>The lane the packet behind the alert travelled in.</summary>
    public PacketLane Lane { get; init; }

    public string Message { get; init; } = string.Empty;

    public DateTime ReceivedUtc { get; init; }
    public DateTime ProcessedUtc { get; init; }

    /// <summary>Time the packet spent queued before it was processed.</summary>
    public int QueueWaitMs { get; init; }

    /// <summary>The automated command the critical lane issued in response, if any.</summary>
    public Guid? AutoCommandId { get; set; }
    public string? AutoCommandSummary { get; set; }
}
