namespace SmartX.Api.Models.Responses;

/// <summary>
/// Overview-level view of a sensor: enough for the grid without pulling the
/// full profile, thresholds and attachments.
/// </summary>
public class SensorListItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public SensorCategory Category { get; set; }
    public string Room { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public SensorStatus Status { get; set; }
    public string FirmwareVersion { get; set; } = string.Empty;
    public DateTime LastSeenUtc { get; set; }
    public bool IsActive { get; set; }
    public double? LatestValue { get; set; }
    public string LatestUnit { get; set; } = string.Empty;
    public ReadingType? PrimaryReadingType { get; set; }
    public int ActiveAlertCount { get; set; }
    public int AnomalyCountLast24h { get; set; }
    public int AttachmentCount { get; set; }
    public List<double> Sparkline { get; set; } = new();
}
