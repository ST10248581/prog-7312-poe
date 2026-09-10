namespace SmartX.Api.Models.Responses;

/// <summary>
/// The overview-first tile row: whole-of-ecosystem health in one payload.
/// </summary>
public class EcosystemSummary
{
    public int TotalSensors { get; set; }
    public int OnlineCount { get; set; }
    public int WarningCount { get; set; }
    public int OfflineCount { get; set; }
    public int ActiveAlertCount { get; set; }
    public int CriticalAlertCount { get; set; }
    public long TotalReadings { get; set; }
    public int ReadingsLastHour { get; set; }
    public int AnomaliesLastHour { get; set; }
    public double MeshHealthScore { get; set; }
    public double AverageProcessingMs { get; set; }
    public double IngestSuccessRate { get; set; }
    public DateTime GeneratedUtc { get; set; }
}
