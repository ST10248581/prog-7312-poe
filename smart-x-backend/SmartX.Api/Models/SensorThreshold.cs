namespace SmartX.Api.Models;

public class SensorThreshold
{
    public Guid Id { get; set; }
    public Guid SensorProfileId { get; set; }
    public ReadingType ReadingType { get; set; }
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }
    public AlertSeverity Severity { get; set; }
    public bool IsEnabled { get; set; }
}
