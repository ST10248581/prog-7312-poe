using SmartX.Api.Models;

namespace SmartX.Api.Data;

/// <summary>
/// In-memory stand-in for the database. Registered as a singleton so every
/// repository and seeder works against the same set of tables.
/// </summary>
public interface ISmartXDataStore
{
    List<SensorProfile> SensorProfiles { get; }
    List<SensorThreshold> SensorThresholds { get; }
    List<TelemetryReading> TelemetryReadings { get; }
    List<Alert> Alerts { get; }
    List<SensorAttachment> SensorAttachments { get; }
    List<IngestionBatch> IngestionBatches { get; }
    List<EngagementState> EngagementStates { get; }

    Dictionary<Guid, byte[]> AttachmentFiles { get; }

    long NextTelemetryReadingId();
}
