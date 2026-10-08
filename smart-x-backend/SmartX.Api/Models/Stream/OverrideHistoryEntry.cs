// =============================================================================
// CODE ATTRIBUTION — Stacks (Part 2)
//
// OverrideHistoryEntry is the element pushed onto the command engine's undo and
// redo stacks, which were written with reference to the source below.
//
// Code Attribution [16]
// Author: Microsoft
// Year: 2025
// Title: Stack<T> Class (System.Collections.Generic)
// Type: [Source code]
// Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.stack-1>
// Accessed: [Accessed 29 September 2026]
// Modifications: OverrideHistoryEntry is the element of the
//   Stack<OverrideHistoryEntry> undo and redo histories. It carries the revert
//   plan worked out when the override was issued, and the
//   UndoOutcome/RedoOutcome enums report what a pop did.
// Reference: Microsoft, 2025. Stack<T> Class (System.Collections.Generic) [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.stack-1> [Accessed 29 September 2026].
// =============================================================================

namespace SmartX.Api.Models.Stream;

public enum UndoOutcome
{
    /// <summary>The override had not left the queue, so it was cancelled outright.</summary>
    Cancelled,

    /// <summary>A compensating command was issued to put the node back.</summary>
    Reverted,

    /// <summary>The override had already acted and has no inverse (a restart, a sample).</summary>
    Irreversible,

    /// <summary>
    /// The override the client asked to undo had already been undone (a double
    /// click, or a retried request). Nothing changed: undo is idempotent.
    /// </summary>
    AlreadyUndone
}

public enum RedoOutcome
{
    /// <summary>The undone override was issued again and is back on the undo stack.</summary>
    Redone,

    /// <summary>It had already been redone. Nothing changed: redo is idempotent.</summary>
    AlreadyRedone
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

    /// <summary>When this entry was created by a redo, the command id of the entry it re-applied.</summary>
    public Guid? RedoOf { get; init; }

    /// <summary>Current status of the original command, filled in when the stack is read.</summary>
    public CommandStatus? Status { get; set; }
}

/// <summary>Both history stacks, top first, so the console can show what undo and redo will act on.</summary>
public class OverrideHistoryResponse
{
    public List<OverrideHistoryEntry> Undo { get; set; } = new();
    public List<OverrideHistoryEntry> Redo { get; set; } = new();
}

public class UndoResult
{
    public UndoOutcome Outcome { get; init; }
    public string Message { get; init; } = string.Empty;
    public OverrideHistoryEntry Undone { get; init; } = new();

    /// <summary>The compensating command, when one was issued.</summary>
    public DeviceCommand? RevertCommand { get; init; }

    public int RemainingDepth { get; init; }
    public int RedoDepth { get; init; }
}

public class RedoResult
{
    public RedoOutcome Outcome { get; init; }
    public string Message { get; init; } = string.Empty;

    /// <summary>The new undo entry the redo created.</summary>
    public OverrideHistoryEntry? Redone { get; init; }

    /// <summary>The command that re-applied the override.</summary>
    public DeviceCommand? Command { get; init; }

    public int UndoDepth { get; init; }
    public int RedoDepth { get; init; }
}
