using SmartX.Api.Data.Collections;
using SmartX.Api.Models;

namespace SmartX.Api.Data;

/// <summary>
/// In-memory stand-in for the database. Registered as a singleton so every
/// repository and seeder works against the same set of tables.
/// <para>
/// The lists are the tables. Alongside them the store keeps indexes - the
/// equivalent of a database's keys - so the hot paths never scan a whole table:
/// dictionaries for O(1) lookup by id, MAC address and node id, and per-sensor
/// <see cref="RingBuffer{T}"/> windows for the "most recent N" questions the
/// dashboard asks on every refresh. Writes that should be indexed go through the
/// Add/Reindex methods below rather than straight into the lists.
/// </para>
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
    List<DeviceCommand> DeviceCommands { get; }

    /// <summary>Encrypted attachment payloads, keyed by attachment id.</summary>
    Dictionary<Guid, byte[]> AttachmentFiles { get; }

    /// <summary>
    /// Guards <see cref="DeviceCommands"/>. Unlike the other tables, the command
    /// log is written by the dispatch simulator on a background timer while
    /// requests are reading it, so every touch of that list takes this lock.
    /// </summary>
    object CommandsSyncRoot { get; }

    long NextTelemetryReadingId();

    // ---------------------------------------------------------------------
    // Indexed reads
    // ---------------------------------------------------------------------

    /// <summary>O(1) profile lookup by id.</summary>
    SensorProfile? FindSensor(Guid id);

    /// <summary>O(1) lookup by MAC address, in any accepted notation.</summary>
    SensorProfile? FindSensorByMac(string macAddress);

    /// <summary>O(1) lookup by node id, ignoring case.</summary>
    SensorProfile? FindSensorByNodeId(string nodeId);

    /// <summary>Every reading for one sensor, in arrival order. Empty if none.</summary>
    IReadOnlyList<TelemetryReading> ReadingsFor(Guid sensorId);

    /// <summary>The bounded window of most recent readings of one type for one sensor.</summary>
    RingBuffer<TelemetryReading>? RecentReadingsFor(Guid sensorId, ReadingType readingType);

    /// <summary>Every recent-reading window for one sensor, one per reading type.</summary>
    IEnumerable<RingBuffer<TelemetryReading>> RecentReadingWindowsFor(Guid sensorId);

    /// <summary>The bounded window of most recent ingest batches for one sensor.</summary>
    RingBuffer<IngestionBatch>? RecentBatchesFor(Guid sensorId);

    // ---------------------------------------------------------------------
    // Indexed writes
    // ---------------------------------------------------------------------

    void AddSensorProfile(SensorProfile profile);

    /// <summary>Re-keys a profile whose MAC address or node id has just changed.</summary>
    void ReindexSensorProfile(SensorProfile profile, string previousMacAddress, string previousNodeId);

    void AddReading(TelemetryReading reading);

    void AddIngestionBatch(IngestionBatch batch);

    /// <summary>
    /// Rebuilds every index from the tables. The seeders write the lists
    /// directly for speed and then call this once.
    /// </summary>
    void RebuildIndexes();
}
