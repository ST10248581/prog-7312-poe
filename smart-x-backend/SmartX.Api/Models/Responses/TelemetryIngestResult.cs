namespace SmartX.Api.Models.Responses;

/// <summary>
/// Per-batch statistics, lifted out of the fixed-size working matrix into a
/// collection the API can serialise.
/// </summary>
public class BatchStatistic
{
    public int BatchIndex { get; set; }
    public int SampleCount { get; set; }
    public int AcceptedCount { get; set; }
    public int RejectedCount { get; set; }
    public int AnomalyCount { get; set; }
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public double MeanValue { get; set; }
}

/// <summary>
/// Result of ingesting a jagged block of historical telemetry batches.
/// </summary>
public class TelemetryIngestResult
{
    public Guid SensorProfileId { get; set; }
    public ReadingType ReadingType { get; set; }
    public string Unit { get; set; } = string.Empty;

    /// <summary>CLR type the gateway payload was wrapped in, e.g. "Single".</summary>
    public string PayloadType { get; set; } = string.Empty;

    public int BatchCount { get; set; }
    public int RawSampleCount { get; set; }
    public int AcceptedCount { get; set; }
    public int RejectedCount { get; set; }
    public int AnomalyCount { get; set; }

    public List<BatchStatistic> BatchStatistics { get; set; } = new();

    public int ProcessingMs { get; set; }
    public DateTime IngestedUtc { get; set; }
}
