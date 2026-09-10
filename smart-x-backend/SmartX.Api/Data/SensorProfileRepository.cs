using SmartX.Api.Models;
using SmartX.Api.Models.Responses;

namespace SmartX.Api.Data;

public class SensorProfileRepository : ISensorProfileRepository
{
    private const int SparklinePoints = 24;
    private const int RecentReadingCount = 25;
    private const int RecentBatchCount = 10;

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
        var sensor = _store.SensorProfiles.FirstOrDefault(profile => profile.Id == id);
        if (sensor is null)
        {
            return null;
        }

        var readings = _store.TelemetryReadings
            .Where(reading => reading.SensorProfileId == id)
            .ToList();

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
            RecentBatches = _store.IngestionBatches
                .Where(batch => batch.SensorProfileId == id)
                .OrderByDescending(batch => batch.ReceivedUtc)
                .Take(RecentBatchCount)
                .ToList(),
            RecentReadings = readings
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

    private SensorListItem BuildListItem(SensorProfile sensor, DateTime cutoff)
    {
        var readings = _store.TelemetryReadings
            .Where(reading => reading.SensorProfileId == sensor.Id)
            .ToList();

        var primaryType = ReadingTypeFor(sensor);

        var primaryReadings = readings
            .Where(reading => reading.ReadingType == primaryType)
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
}
