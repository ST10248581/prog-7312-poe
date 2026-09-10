namespace SmartX.Api.Models;

public class TelemetryQuery
{
    public List<Guid>? SensorProfileIds { get; set; }
    public List<SensorCategory>? Categories { get; set; }
    public List<string>? Zones { get; set; }
    public List<SensorStatus>? Statuses { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public bool AnomaliesOnly { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
