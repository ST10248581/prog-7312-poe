namespace SmartX.Api.Models.Stream;

public enum UndoOutcome
{
    /// <summary>The override had not left the queue, so it was cancelled outright.</summary>
    Cancelled,

    /// <summary>A compensating command was issued to put the node back.</summary>
    Reverted,

    /// <summary>The override had already acted and has no inverse (a restart, a sample).</summary>
    Irreversible
}

/// <summary>
/// One manual override on the undo stack, with the plan for reversing it worked
/// out when it was issued — the previous value is only knowable at that moment.
/// </summary>
public class OverrideHistoryEntry
{
    public Guid CommandId { get; init; }
    public string NodeId { get; init; } = string.Empty;
    public string SensorName { get; init; } = string.Empty;
    public CommandType CommandType { get; init; }
    public string Parameters { get; init; } = string.Empty;
    public CommandPriority Priority { get; init; }
    public string IssuedBy { get; init; } = string.Empty;
    public DateTime IssuedUtc { get; init; }

    /// <summary>The compensating command; null when the override has no inverse.</summary>
    public CommandType? RevertCommandType { get; init; }
    public string? RevertParameters { get; init; }

    /// <summary>What undo will do, in words, e.g. "Restore temp.max to 26.5".</summary>
    public string UndoDescription { get; init; } = string.Empty;

    /// <summary>Current status of the original command, filled in when the stack is read.</summary>
    public CommandStatus? Status { get; set; }
}

public class UndoResult
{
    public UndoOutcome Outcome { get; init; }
    public string Message { get; init; } = string.Empty;
    public OverrideHistoryEntry Undone { get; init; } = new();

    /// <summary>The compensating command, when one was issued.</summary>
    public DeviceCommand? RevertCommand { get; init; }

    public int RemainingDepth { get; init; }
}
