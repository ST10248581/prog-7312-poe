// =============================================================================
// CODE ATTRIBUTION — Sorted lists (Part 2)
//
// SensorLogEntry is the value stored in each node's time-ordered log, which was
// written with reference to the source below.
//
// Code Attribution [18]
// Author: Microsoft
// Year: 2025
// Title: SortedList<TKey,TValue> Class (System.Collections.Generic)
// Type: [Source code]
// Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.sortedlist-2>
// Accessed: [Accessed 8 October 2026]
// Modifications: SensorLogEntry is the value type of the per-node
//   SortedList<DateTime, SensorLogEntry>; readings, commands, alerts and link
//   changes share one log so the timeline renders them against the same clock.
// Reference: Microsoft, 2025. SortedList<TKey,TValue> Class (System.Collections.Generic) [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.sortedlist-2> [Accessed 8 October 2026].
// =============================================================================

namespace SmartX.Api.Models.Stream;

public enum SensorLogKind
{
    Reading,
    Command,
    Alert,
    Recovered,
    Disconnected,
    Reconnected
}

/// <summary>
/// One line of a node's historical log. Readings, commands and alert events
/// share one log per node so the timeline renders them against the same clock.
/// </summary>
public class SensorLogEntry
{
    public DateTime TimestampUtc { get; init; }
    public SensorLogKind Kind { get; init; }
    public ReadingType? ReadingType { get; init; }
    public double? Value { get; init; }
    public string Label { get; init; } = string.Empty;
    public AlertSeverity? Severity { get; init; }
}
