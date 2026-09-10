namespace SmartX.Api.Models;

public class IngestionBatch
{
    public Guid Id { get; set; }
    public Guid SensorProfileId { get; set; }
    public DateTime ReceivedUtc { get; set; }
    public int ReadingCount { get; set; }
    public int AcceptedCount { get; set; }
    public int RejectedCount { get; set; }
    public string SourceIpAddress { get; set; } = string.Empty;
    public int ProcessingMs { get; set; }
}
