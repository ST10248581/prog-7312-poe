namespace SmartX.Api.Models.Responses;

public class SensorSeriesPoint
{
    public DateTime TimestampUtc { get; set; }
    public double? Value { get; set; }
    public bool? BooleanValue { get; set; }
    public bool IsAnomaly { get; set; }
    public ReadingQuality Quality { get; set; }
}

/// <summary>
/// A single chart line, carrying its own threshold markers so the frontend can
/// draw the limits without a second request.
/// </summary>
public class SensorSeries
{
    public Guid SensorProfileId { get; set; }
    public string SensorName { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public ReadingType ReadingType { get; set; }
    public string Unit { get; set; } = string.Empty;
    public double? MinThreshold { get; set; }
    public double? MaxThreshold { get; set; }
    public double? LatestValue { get; set; }
    public int AnomalyCount { get; set; }

    /// <summary>True when the points predate the requested window because the sensor stopped reporting.</summary>
    public bool IsStale { get; set; }

    public DateTime? LastReadingUtc { get; set; }
    public List<SensorSeriesPoint> Points { get; set; } = new();
}
