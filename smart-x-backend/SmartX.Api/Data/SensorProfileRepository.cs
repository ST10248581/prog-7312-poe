using SmartX.Api.Models;
using SmartX.Api.Models.Requests;
using SmartX.Api.Models.Validation;
using SmartX.Api.Models.Responses;

namespace SmartX.Api.Data;

public class SensorProfileRepository : ISensorProfileRepository
{
    private const int SparklinePoints = 24;
    private const int RecentReadingCount = 25;

    private readonly ISmartXDataStore _store;

    public SensorProfileRepository(ISmartXDataStore store)
    {
        _store = store;
    }

    public List<SensorListItem> GetAll(TelemetryQuery query)
    {
        var sensors = _store.SensorProfiles.AsEnumerable();

        if (query.SensorProfileIds is { Count: > 0 })
        {
            sensors = sensors.Where(sensor => query.SensorProfileIds.Contains(sensor.Id));
        }

        if (query.Categories is { Count: > 0 })
        {
            sensors = sensors.Where(sensor => query.Categories.Contains(sensor.Category));
        }

        if (query.Statuses is { Count: > 0 })
        {
            sensors = sensors.Where(sensor => query.Statuses.Contains(sensor.Status));
        }

        if (query.Zones is { Count: > 0 })
        {
            sensors = sensors.Where(sensor => query.Zones.Contains(sensor.Zone));
        }

        var cutoff = DateTime.UtcNow.AddHours(-24);

        var items = sensors
            .Select(sensor => BuildListItem(sensor, cutoff))
            .ToList();

        if (query.AnomaliesOnly)
        {
            items = items.Where(item => item.AnomalyCountLast24h > 0).ToList();
        }

        // Worst-first: offline before warning before online, then by alert weight.
        return items
            .OrderByDescending(item => (int)item.Status)
            .ThenByDescending(item => item.ActiveAlertCount)
            .ThenBy(item => item.NodeId)
            .ToList();
    }

    public SensorDetail? GetDetail(Guid id)
    {
        var sensor = _store.FindSensor(id);
        if (sensor is null)
        {
            return null;
        }

        var thresholds = _store.SensorThresholds
            .Where(threshold => threshold.SensorProfileId == id)
            .ToList();

        return new SensorDetail
        {
            Profile = sensor,
            Thresholds = thresholds,
            Attachments = _store.SensorAttachments
                .Where(attachment => attachment.SensorProfileId == id)
                .OrderByDescending(attachment => attachment.UploadedUtc)
                .ToList(),
            // The batch window already holds exactly the last ten ingests.
            RecentBatches = _store.RecentBatchesFor(id)?.NewestFirst().ToList() ?? new List<IngestionBatch>(),
            // Merge the per-type windows rather than sorting every reading the sensor has sent.
            RecentReadings = _store.RecentReadingWindowsFor(id)
                .SelectMany(window => window)
                .OrderByDescending(reading => reading.TimestampUtc)
                .Take(RecentReadingCount)
                .ToList(),
            Alerts = _store.Alerts
                .Where(alert => alert.SensorProfileId == id)
                .OrderByDescending(alert => alert.TriggeredUtc)
                .ToList()
        };
    }

    public FilterOptions GetFilterOptions()
    {
        return new FilterOptions
        {
            Categories = Enum.GetNames<SensorCategory>().ToList(),
            Statuses = Enum.GetNames<SensorStatus>().ToList(),
            ReadingTypes = Enum.GetNames<ReadingType>().ToList(),
            Zones = _store.SensorProfiles.Select(sensor => sensor.Zone).Distinct().Order().ToList(),
            Rooms = _store.SensorProfiles.Select(sensor => sensor.Room).Distinct().Order().ToList()
        };
    }

    public SensorProfile? UpdatePayload(Guid id, UpdateSensorPayloadRequest request)
    {
        var sensor = _store.FindSensor(id);
        if (sensor is null)
        {
            return null;
        }

        var previousMac = sensor.MacAddress;
        var previousNodeId = sensor.NodeId;

        sensor.MacAddress = MacAddress.Normalise(request.MacAddress) ?? request.MacAddress.Trim();
        sensor.Room = request.Room.Trim();
        sensor.Zone = request.Zone.Trim();
        sensor.NodeId = request.NodeId.Trim().ToUpperInvariant();
        sensor.Category = request.Category;

        _store.ReindexSensorProfile(sensor, previousMac, previousNodeId);
        return sensor;
    }

    public SensorProfile Create(CreateSensorRequest request)
    {
        var sensor = new SensorProfile
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            // Stored in one canonical form so the same device cannot register twice
            // under a different notation.
            MacAddress = MacAddress.Normalise(request.MacAddress) ?? request.MacAddress.Trim(),
            SerialNumber = $"SN-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            Room = request.Room.Trim(),
            Zone = request.Zone.Trim(),
            NodeId = request.NodeId.Trim().ToUpperInvariant(),
            Category = request.Category,
            Status = SensorStatus.Offline,
            FirmwareVersion = "1.0.0",
            RegisteredUtc = DateTime.UtcNow,
            LastSeenUtc = DateTime.UtcNow,
            IsActive = true
        };

        _store.AddSensorProfile(sensor);
        return sensor;
    }

    public SensorAttachment AddAttachment(Guid sensorId, SensorAttachment attachment, byte[] sealedPayload)
    {
        attachment.SensorProfileId = sensorId;
        lock (_store.AttachmentFiles)
        {
            _store.AttachmentFiles[attachment.Id] = sealedPayload;
            _store.SensorAttachments.Add(attachment);
        }

        return attachment;
    }

    public (SensorAttachment Attachment, byte[]? SealedPayload)? GetAttachment(Guid sensorId, Guid attachmentId)
    {
        var attachment = _store.SensorAttachments
            .FirstOrDefault(a => a.Id == attachmentId && a.SensorProfileId == sensorId);

        if (attachment is null)
        {
            return null;
        }

        return (attachment, _store.AttachmentFiles.GetValueOrDefault(attachmentId));
    }

    private SensorListItem BuildListItem(SensorProfile sensor, DateTime cutoff)
    {
        // Only this sensor's readings, from the per-sensor index, rather than a
        // scan of every reading in the mesh for every card.
        var readings = _store.ReadingsFor(sensor.Id);

        var primaryType = ReadingTypeFor(sensor);

        // The sparkline and latest value come from the bounded ring window: its
        // size is fixed however long the sensor has been reporting. Ordered by
        // timestamp because a gateway may backfill older samples after newer ones.
        var primaryReadings = (_store.RecentReadingsFor(sensor.Id, primaryType) ?? Enumerable.Empty<TelemetryReading>())
            .OrderBy(reading => reading.TimestampUtc)
            .ToList();

        var latest = primaryReadings.LastOrDefault();

        return new SensorListItem
        {
            Id = sensor.Id,
            Name = sensor.Name,
            NodeId = sensor.NodeId,
            MacAddress = sensor.MacAddress,
            Category = sensor.Category,
            Room = sensor.Room,
            Zone = sensor.Zone,
            Status = sensor.Status,
            FirmwareVersion = sensor.FirmwareVersion,
            LastSeenUtc = sensor.LastSeenUtc,
            IsActive = sensor.IsActive,
            PrimaryReadingType = primaryType,
            LatestValue = latest?.NumericValue ?? (latest?.BooleanValue == true ? 1 : latest?.BooleanValue == false ? 0 : null),
            LatestUnit = latest?.Unit ?? string.Empty,
            ActiveAlertCount = _store.Alerts.Count(alert =>
                alert.SensorProfileId == sensor.Id && alert.Status == AlertStatus.Active),
            AnomalyCountLast24h = readings.Count(reading =>
                reading.IsAnomaly && reading.TimestampUtc >= cutoff),
            AttachmentCount = _store.SensorAttachments.Count(attachment =>
                attachment.SensorProfileId == sensor.Id),
            Sparkline = BuildSparkline(primaryReadings)
        };
    }

    private static ReadingType ReadingTypeFor(SensorProfile sensor)
    {
        return Seeding.ReadingTypeProfile.ForCategory(sensor.Category).First();
    }

    private static List<double> BuildSparkline(List<TelemetryReading> readings)
    {
        return readings
            .TakeLast(SparklinePoints)
            .Select(reading => reading.NumericValue ?? (reading.BooleanValue == true ? 1 : 0))
            .ToList();
    }

    public List<SensorProfile> GetProfiles()
    {
        return _store.SensorProfiles.ToList();
    }

    public SensorProfile? GetById(Guid id)
    {
        return _store.FindSensor(id);
    }

    public SensorProfile? GetByMacAddress(string macAddress)
    {
        return _store.FindSensorByMac(macAddress);
    }

    public SensorProfile? GetByNodeId(string nodeId)
    {
        return _store.FindSensorByNodeId(nodeId);
    }
}
