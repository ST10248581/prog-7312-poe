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
