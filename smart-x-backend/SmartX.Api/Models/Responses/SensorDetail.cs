namespace SmartX.Api.Models.Responses;

/// <summary>
/// Details-on-demand payload: everything needed to investigate one sensor.
/// </summary>
public class SensorDetail
{
    public SensorProfile Profile { get; set; } = new();
    public List<SensorThreshold> Thresholds { get; set; } = new();
    public List<SensorAttachment> Attachments { get; set; } = new();
    public List<IngestionBatch> RecentBatches { get; set; } = new();
    public List<TelemetryReading> RecentReadings { get; set; } = new();
    public List<Alert> Alerts { get; set; } = new();
    public List<SensorSeries> Series { get; set; } = new();
}
