namespace SmartX.Api.Models.Stream;

/// <summary>
/// A node's log over a window, already in timestamp order and down-sampled, so
/// the dashboard draws it without sorting or thinning anything itself.
/// </summary>
public class NodeTimeline
{
    public string NodeId { get; set; } = string.Empty;
    public string SensorName { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public int WindowMinutes { get; set; }
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }

    /// <summary>Entries held in the node's sorted log, across all kinds.</summary>
    public int LogSize { get; set; }

    public bool IsDisconnected { get; set; }

    /// <summary>Log entries inside the window, found by binary search on the sorted keys.</summary>
    public int EntriesInWindow { get; set; }

    /// <summary>Older entries the range read jumped over without visiting.</summary>
    public int EntriesSkipped { get; set; }

    /// <summary>Time taken to locate and copy the window out of the sorted log.</summary>
    public double RangeReadMicroseconds { get; set; }

    public List<TimelineSeries> Series { get; set; } = new();

    /// <summary>Commands, alerts and link changes, oldest first.</summary>
    public List<SensorLogEntry> Events { get; set; } = new();
}

public class TimelineSeries
{
    public ReadingType ReadingType { get; set; }
    public string Unit { get; set; } = string.Empty;
    public double? MinThreshold { get; set; }
    public double? MaxThreshold { get; set; }
    public List<TimelinePoint> Points { get; set; } = new();
}

public class TimelinePoint
{
    public DateTime TimestampUtc { get; set; }
    public double Value { get; set; }
}
