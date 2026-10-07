using SmartX.Api.Data.Collections;
using SmartX.Api.Models;
using SmartX.Api.Models.Validation;

namespace SmartX.Api.Data;

public class SmartXDataStore : ISmartXDataStore
{
    /// <summary>Readings kept per sensor and type for sparklines and "latest value".</summary>
    public const int RecentReadingWindow = 64;

    /// <summary>Ingest batches kept per sensor for the detail view's ingestion tab.</summary>
    public const int RecentBatchWindow = 10;

    private long _telemetryReadingId;

    // Every index write takes this lock, so two requests registering or
    // ingesting at once cannot leave a dictionary half-updated.
    private readonly object _indexSync = new();

    private readonly Dictionary<Guid, SensorProfile> _sensorsById = new();
    private readonly Dictionary<string, SensorProfile> _sensorsByMac = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SensorProfile> _sensorsByNodeId = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Guid, List<TelemetryReading>> _readingsBySensor = new();
    private readonly Dictionary<(Guid SensorId, ReadingType Type), RingBuffer<TelemetryReading>> _recentReadings = new();
    private readonly Dictionary<Guid, RingBuffer<IngestionBatch>> _recentBatches = new();

    public List<SensorProfile> SensorProfiles { get; } = new();
    public List<SensorThreshold> SensorThresholds { get; } = new();
    public List<TelemetryReading> TelemetryReadings { get; } = new();
    public List<Alert> Alerts { get; } = new();
    public List<SensorAttachment> SensorAttachments { get; } = new();
    public List<IngestionBatch> IngestionBatches { get; } = new();
    public List<EngagementState> EngagementStates { get; } = new();
    public List<DeviceCommand> DeviceCommands { get; } = new();

    public Dictionary<Guid, byte[]> AttachmentFiles { get; } = new();

    public object CommandsSyncRoot { get; } = new();

    public long NextTelemetryReadingId()
    {
        return Interlocked.Increment(ref _telemetryReadingId);
    }

    // =====================================================================
    // Indexed reads
    // =====================================================================

    public SensorProfile? FindSensor(Guid id)
    {
        return _sensorsById.GetValueOrDefault(id);
    }

    public SensorProfile? FindSensorByMac(string macAddress)
    {
        var key = MacAddress.Normalise(macAddress);
        return key is null ? null : _sensorsByMac.GetValueOrDefault(key);
    }

    public SensorProfile? FindSensorByNodeId(string nodeId)
    {
        return string.IsNullOrWhiteSpace(nodeId) ? null : _sensorsByNodeId.GetValueOrDefault(nodeId.Trim());
    }

    public IReadOnlyList<TelemetryReading> ReadingsFor(Guid sensorId)
    {
        return _readingsBySensor.TryGetValue(sensorId, out var readings)
            ? readings
            : Array.Empty<TelemetryReading>();
    }

    public RingBuffer<TelemetryReading>? RecentReadingsFor(Guid sensorId, ReadingType readingType)
    {
        return _recentReadings.GetValueOrDefault((sensorId, readingType));
    }

    public IEnumerable<RingBuffer<TelemetryReading>> RecentReadingWindowsFor(Guid sensorId)
    {
        foreach (var readingType in Enum.GetValues<ReadingType>())
        {
            if (_recentReadings.TryGetValue((sensorId, readingType), out var window))
            {
                yield return window;
            }
        }
    }

    public RingBuffer<IngestionBatch>? RecentBatchesFor(Guid sensorId)
    {
        return _recentBatches.GetValueOrDefault(sensorId);
    }

    // =====================================================================
    // Indexed writes
    // =====================================================================

    public void AddSensorProfile(SensorProfile profile)
    {
        lock (_indexSync)
        {
            SensorProfiles.Add(profile);
            IndexSensor(profile);
        }
    }

    public void ReindexSensorProfile(SensorProfile profile, string previousMacAddress, string previousNodeId)
    {
        lock (_indexSync)
        {
            var previousMacKey = MacAddress.Normalise(previousMacAddress);
            if (previousMacKey is not null && _sensorsByMac.GetValueOrDefault(previousMacKey) == profile)
            {
                _sensorsByMac.Remove(previousMacKey);
            }

            if (_sensorsByNodeId.GetValueOrDefault(previousNodeId) == profile)
            {
                _sensorsByNodeId.Remove(previousNodeId);
            }

            IndexSensor(profile);
        }
    }

    public void AddReading(TelemetryReading reading)
    {
        lock (_indexSync)
        {
            TelemetryReadings.Add(reading);
            IndexReading(reading);
        }
    }

    public void AddIngestionBatch(IngestionBatch batch)
    {
        lock (_indexSync)
        {
            IngestionBatches.Add(batch);
            IndexBatch(batch);
        }
    }

    public void RebuildIndexes()
    {
        lock (_indexSync)
        {
            _sensorsById.Clear();
            _sensorsByMac.Clear();
            _sensorsByNodeId.Clear();
            _readingsBySensor.Clear();
            _recentReadings.Clear();
            _recentBatches.Clear();

            foreach (var profile in SensorProfiles)
            {
                IndexSensor(profile);
            }

            // The seeder sorts readings oldest first, so each window fills in
            // time order and ends holding the newest readings.
            foreach (var reading in TelemetryReadings)
            {
                IndexReading(reading);
            }

            // Batches are seeded newest first; feed them oldest first for the same reason.
            for (var index = IngestionBatches.Count - 1; index >= 0; index--)
            {
                IndexBatch(IngestionBatches[index]);
            }
        }
    }

    private void IndexSensor(SensorProfile profile)
    {
        _sensorsById[profile.Id] = profile;

        var macKey = MacAddress.Normalise(profile.MacAddress);
        if (macKey is not null)
        {
            _sensorsByMac[macKey] = profile;
        }

        if (!string.IsNullOrWhiteSpace(profile.NodeId))
        {
            _sensorsByNodeId[profile.NodeId] = profile;
        }
    }

    private void IndexReading(TelemetryReading reading)
    {
        if (!_readingsBySensor.TryGetValue(reading.SensorProfileId, out var readings))
        {
            readings = new List<TelemetryReading>();
            _readingsBySensor[reading.SensorProfileId] = readings;
        }

        readings.Add(reading);

        var key = (reading.SensorProfileId, reading.ReadingType);
        if (!_recentReadings.TryGetValue(key, out var window))
        {
            window = new RingBuffer<TelemetryReading>(RecentReadingWindow);
            _recentReadings[key] = window;
        }

        // O(1): once the window is full the oldest reading drops out.
        window.Add(reading);
    }

    private void IndexBatch(IngestionBatch batch)
    {
        if (!_recentBatches.TryGetValue(batch.SensorProfileId, out var window))
        {
            window = new RingBuffer<IngestionBatch>(RecentBatchWindow);
            _recentBatches[batch.SensorProfileId] = window;
        }

        window.Add(batch);
    }
}
