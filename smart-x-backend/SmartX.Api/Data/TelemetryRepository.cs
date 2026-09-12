using SmartX.Api.Models;
using SmartX.Api.Models.Responses;

namespace SmartX.Api.Data;

public class TelemetryRepository : ITelemetryRepository
{
    private readonly ISmartXDataStore _store;

    public TelemetryRepository(ISmartXDataStore store)
    {
        _store = store;
    }

    public PagedResult<TelemetryReading> Query(TelemetryQuery query)
    {
        var sensorIds = ResolveSensorIds(query);
        var readings = _store.TelemetryReadings.AsEnumerable();

        if (sensorIds is not null)
        {
            readings = readings.Where(reading => sensorIds.Contains(reading.SensorProfileId));
        }

        if (query.FromUtc.HasValue)
        {
            readings = readings.Where(reading => reading.TimestampUtc >= query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            readings = readings.Where(reading => reading.TimestampUtc <= query.ToUtc.Value);
        }

        if (query.AnomaliesOnly)
        {
            readings = readings.Where(reading => reading.IsAnomaly);
        }

        var ordered = readings.OrderByDescending(reading => reading.TimestampUtc).ToList();

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 500);

        return new PagedResult<TelemetryReading>
        {
            Items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = ordered.Count,
            TotalPages = (int)Math.Ceiling(ordered.Count / (double)pageSize)
        };
    }

    public List<SensorSeries> GetSeries(Guid sensorProfileId, DateTime? fromUtc, int maxPoints)
    {
        var sensor = _store.SensorProfiles.FirstOrDefault(profile => profile.Id == sensorProfileId);
        if (sensor is null)
        {
            return new List<SensorSeries>();
        }

        var now = DateTime.UtcNow;
        var from = fromUtc ?? now.AddHours(-24);

        var all = _store.TelemetryReadings
            .Where(reading => reading.SensorProfileId == sensorProfileId)
            .OrderBy(reading => reading.TimestampUtc)
            .ToList();

        var readings = all.Where(reading => reading.TimestampUtc >= from).ToList();
        var isStale = false;

        // An offline sensor has nothing inside the live window. Fall back to the
        // same span ending at its last reading so the run-up to the disconnection
        // is still visible when investigating.
        if (readings.Count == 0 && all.Count > 0)
        {
            var lastTimestamp = all[^1].TimestampUtc;
            var fallbackFrom = lastTimestamp - (now - from);
            readings = all.Where(reading => reading.TimestampUtc >= fallbackFrom).ToList();
            isStale = true;
        }

        return readings
            .GroupBy(reading => reading.ReadingType)
            .Select(group => BuildSeries(sensor, group.Key, group.ToList(), maxPoints, isStale))
            .ToList();
    }

    public EcosystemSummary GetSummary()
    {
        var now = DateTime.UtcNow;
        var hourAgo = now.AddHours(-1);

        var recent = _store.TelemetryReadings
            .Where(reading => reading.TimestampUtc >= hourAgo)
            .ToList();

        var sensorCount = _store.SensorProfiles.Count;
        var online = _store.SensorProfiles.Count(sensor => sensor.Status == SensorStatus.Online);
        var warning = _store.SensorProfiles.Count(sensor => sensor.Status == SensorStatus.Warning);

        var totalIngested = _store.IngestionBatches.Sum(batch => (long)batch.ReadingCount);
        var totalAccepted = _store.IngestionBatches.Sum(batch => (long)batch.AcceptedCount);

        return new EcosystemSummary
        {
            TotalSensors = sensorCount,
            OnlineCount = online,
            WarningCount = warning,
            OfflineCount = _store.SensorProfiles.Count(sensor => sensor.Status == SensorStatus.Offline),
            ActiveAlertCount = _store.Alerts.Count(alert => alert.Status == AlertStatus.Active),
            CriticalAlertCount = _store.Alerts.Count(alert =>
                alert.Status == AlertStatus.Active && alert.Severity == AlertSeverity.Critical),
            TotalReadings = _store.TelemetryReadings.Count,
            ReadingsLastHour = recent.Count,
            AnomaliesLastHour = recent.Count(reading => reading.IsAnomaly),
            MeshHealthScore = sensorCount == 0
                ? 0
                : Math.Round((online + warning * 0.5) / sensorCount * 100, 1),
            AverageProcessingMs = _store.IngestionBatches.Count == 0
                ? 0
                : Math.Round(_store.IngestionBatches.Average(batch => batch.ProcessingMs), 1),
            IngestSuccessRate = totalIngested == 0
                ? 100
                : Math.Round(totalAccepted / (double)totalIngested * 100, 2),
            GeneratedUtc = now
        };
    }

    private HashSet<Guid>? ResolveSensorIds(TelemetryQuery query)
    {
        var hasSensorFilter = query.SensorProfileIds is { Count: > 0 };
        var hasCategoryFilter = query.Categories is { Count: > 0 };
        var hasZoneFilter = query.Zones is { Count: > 0 };
        var hasStatusFilter = query.Statuses is { Count: > 0 };

        if (!hasSensorFilter && !hasCategoryFilter && !hasZoneFilter && !hasStatusFilter)
        {
            return null;
        }

        var sensors = _store.SensorProfiles.AsEnumerable();

        if (hasSensorFilter)
        {
            sensors = sensors.Where(sensor => query.SensorProfileIds!.Contains(sensor.Id));
        }

        if (hasCategoryFilter)
        {
            sensors = sensors.Where(sensor => query.Categories!.Contains(sensor.Category));
        }

        if (hasZoneFilter)
        {
            sensors = sensors.Where(sensor => query.Zones!.Contains(sensor.Zone));
        }

        if (hasStatusFilter)
        {
            sensors = sensors.Where(sensor => query.Statuses!.Contains(sensor.Status));
        }

        return sensors.Select(sensor => sensor.Id).ToHashSet();
    }

    private SensorSeries BuildSeries(
        SensorProfile sensor,
        ReadingType readingType,
        List<TelemetryReading> readings,
        int maxPoints,
        bool isStale)
    {
        var threshold = _store.SensorThresholds.FirstOrDefault(item =>
            item.SensorProfileId == sensor.Id && item.ReadingType == readingType);

        // Thin the series so a week of data still draws smoothly.
        var step = maxPoints > 0 && readings.Count > maxPoints
            ? (int)Math.Ceiling(readings.Count / (double)maxPoints)
            : 1;

        var points = readings
            .Where((_, index) => index % step == 0)
            .Select(reading => new SensorSeriesPoint
            {
                TimestampUtc = reading.TimestampUtc,
                Value = reading.NumericValue,
                BooleanValue = reading.BooleanValue,
                IsAnomaly = reading.IsAnomaly,
                Quality = reading.Quality
            })
            .ToList();

        var last = readings.LastOrDefault();

        return new SensorSeries
        {
            SensorProfileId = sensor.Id,
            SensorName = sensor.Name,
            NodeId = sensor.NodeId,
            ReadingType = readingType,
            Unit = readings.FirstOrDefault()?.Unit ?? string.Empty,
            MinThreshold = threshold is { IsEnabled: true } ? threshold.MinValue : null,
            MaxThreshold = threshold is { IsEnabled: true } ? threshold.MaxValue : null,
            LatestValue = last?.NumericValue ?? (last?.BooleanValue == true ? 1 : last?.BooleanValue == false ? 0 : null),
            AnomalyCount = readings.Count(reading => reading.IsAnomaly),
            IsStale = isStale,
            LastReadingUtc = last?.TimestampUtc,
            Points = points
        };
    }

    public TelemetryReading? GetLatest(Guid sensorProfileId, ReadingType readingType)
    {
        return _store.TelemetryReadings
            .Where(reading => reading.SensorProfileId == sensorProfileId && reading.ReadingType == readingType)
            .OrderByDescending(reading => reading.TimestampUtc)
            .FirstOrDefault();
    }

    public int AppendReadings(IReadOnlyList<TelemetryReading> readings, IngestionBatch batch)
    {
        foreach (var reading in readings)
        {
            reading.Id = _store.NextTelemetryReadingId();
            _store.TelemetryReadings.Add(reading);
        }

        _store.IngestionBatches.Add(batch);

        // The sensor has just reported, so its liveness marker moves with it.
        var sensor = _store.SensorProfiles.FirstOrDefault(profile => profile.Id == batch.SensorProfileId);
        if (sensor is not null && readings.Count > 0)
        {
            var newest = readings.Max(reading => reading.TimestampUtc);
            if (newest > sensor.LastSeenUtc)
            {
                sensor.LastSeenUtc = newest;
            }
        }

        return readings.Count;
    }
}
