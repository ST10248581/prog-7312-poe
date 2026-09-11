using SmartX.Api.Models;

namespace SmartX.Api.Data;

public class SmartXDataStore : ISmartXDataStore
{
    private long _telemetryReadingId;

    public List<SensorProfile> SensorProfiles { get; } = new();
    public List<SensorThreshold> SensorThresholds { get; } = new();
    public List<TelemetryReading> TelemetryReadings { get; } = new();
    public List<Alert> Alerts { get; } = new();
    public List<SensorAttachment> SensorAttachments { get; } = new();
    public List<IngestionBatch> IngestionBatches { get; } = new();
    public List<EngagementState> EngagementStates { get; } = new();

    public Dictionary<Guid, byte[]> AttachmentFiles { get; } = new();

    public long NextTelemetryReadingId()
    {
        return Interlocked.Increment(ref _telemetryReadingId);
    }
}
