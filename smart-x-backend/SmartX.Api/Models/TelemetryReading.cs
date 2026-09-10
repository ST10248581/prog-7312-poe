namespace SmartX.Api.Models;

public class TelemetryReading
{
    public long Id { get; set; }
    public Guid SensorProfileId { get; set; }
    public ReadingType ReadingType { get; set; }
    public double? NumericValue { get; set; }
    public string? TextValue { get; set; }
    public bool? BooleanValue { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
    public ReadingQuality Quality { get; set; }
    public bool IsAnomaly { get; set; }
}
