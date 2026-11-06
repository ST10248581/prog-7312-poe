namespace SmartX.Api.Models.Topology;

public enum HistorySort
{
    Newest,
    Oldest
}

public enum HistoryEntryStatus
{
    Normal,

    /// <summary>Outside the sensor's configured limits, or flagged anomalous at ingest.</summary>
    Breach,

    Suspect,
    Bad
}

public enum HistorySource
{
    /// <summary>The stored reading history (seeded, or ingested on page 1).</summary>
    Archive,

    /// <summary>The live intake's per-node sorted log (page 2).</summary>
    Live
}

/// <summary>
/// One sensor's telemetry over a window, merged from the stored history and the
/// live intake, ordered by time and paged, with the route each reading
/// travelled at the time it was taken.
/// </summary>
public class TelemetryHistory
{
    public string NodeId { get; set; } = string.Empty;
    public Guid SensorProfileId { get; set; }
    public string SensorName { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;

    public int Hours { get; set; }
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }
    public HistorySort Sort { get; set; }
    public ReadingType? ReadingType { get; set; }

    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    public List<TelemetryHistoryEntry> Items { get; set; } = new();

    /// <summary>Per-metric figures over the whole window, not just the page.</summary>
    public List<HistoryMetricSummary> Metrics { get; set; } = new();

    /// <summary>Times the node's route changed inside the window, oldest first.</summary>
    public List<RouteChange> RouteChanges { get; set; } = new();

    /// <summary>Reading types this sensor reports, for the metric filter.</summary>
    public List<ReadingType> AvailableReadingTypes { get; set; } = new();

    /// <summary>True when an explicit from/to range was queried rather than "the last N hours".</summary>
    public bool IsCustomRange { get; set; }

    /// <summary>Time taken to cut the window out of the AVL trees, merge and page it.</summary>
    public double WindowReadMicroseconds { get; set; }

    /* ---------- AVL index figures: how little of the tree the query read ---------- */

    /// <summary>Stored readings indexed in the trees that were queried.</summary>
    public int IndexedReadings { get; set; }

    /// <summary>Tree nodes the range search touched — the boundary paths plus the results.</summary>
    public int NodesVisited { get; set; }

    /// <summary>Height of the tallest tree queried: about log₂ of its size, because AVL keeps it balanced.</summary>
    public int TreeHeight { get; set; }
}

public class TelemetryHistoryEntry
{
    public DateTime TimestampUtc { get; set; }
    public ReadingType ReadingType { get; set; }
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public HistoryEntryStatus Status { get; set; }
    public HistorySource Source { get; set; }

    /// <summary>Hops to the gateway in effect when the reading was taken. Null when the node had no route.</summary>
    public int? RouteHops { get; set; }

    public string? RouteGatewayId { get; set; }
}

public class HistoryMetricSummary
{
    public ReadingType ReadingType { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public double Average { get; set; }
    public int Breaches { get; set; }
}

public class RouteChange
{
    public DateTime TimestampUtc { get; set; }
    public int? HopsBefore { get; set; }
    public int? HopsAfter { get; set; }
    public string? GatewayAfter { get; set; }
    public string Description { get; set; } = string.Empty;
}
