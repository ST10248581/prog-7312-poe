namespace SmartX.Api.Models;

public class EngagementState
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int SensorsRegistered { get; set; }
    public int ReadingsIngested { get; set; }
    public int AlertsResolved { get; set; }
    public int AttachmentsUploaded { get; set; }
    public double MeshHealthScore { get; set; }
    public DateTime LastUpdatedUtc { get; set; }
}
