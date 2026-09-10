namespace SmartX.Api.Models;

public class Alert
{
    public Guid Id { get; set; }
    public Guid SensorProfileId { get; set; }
    public long? TelemetryReadingId { get; set; }
    public AlertType AlertType { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime TriggeredUtc { get; set; }
    public DateTime? AcknowledgedUtc { get; set; }
    public AlertStatus Status { get; set; }
}
