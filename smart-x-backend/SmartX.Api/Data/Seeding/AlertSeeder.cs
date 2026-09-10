using SmartX.Api.Models;

namespace SmartX.Api.Data.Seeding;

/// <summary>
/// Builds alerts from the seeded telemetry so the alert list lines up with what
/// the charts actually show, then adds device-level alerts on top.
/// </summary>
public class AlertSeeder : IAlertSeeder
{
    private const int MaxThresholdAlertsPerSensor = 12;

    private readonly ISmartXDataStore _store;
    private readonly SeedOptions _options;

    public AlertSeeder(ISmartXDataStore store, SeedOptions options)
    {
        _store = store;
        _options = options;
    }

    public void Seed()
    {
        if (_store.Alerts.Count > 0)
        {
            return;
        }

        var random = new Random(_options.RandomSeed + 3);
        var now = DateTime.UtcNow;
        var sensorsById = _store.SensorProfiles.ToDictionary(sensor => sensor.Id);

        var anomaliesBySensor = _store.TelemetryReadings
            .Where(reading => reading.IsAnomaly)
            .GroupBy(reading => reading.SensorProfileId);

        foreach (var group in anomaliesBySensor)
        {
            if (!sensorsById.TryGetValue(group.Key, out var sensor))
            {
                continue;
            }

            var recent = group
                .OrderByDescending(reading => reading.TimestampUtc)
                .Take(MaxThresholdAlertsPerSensor);

            foreach (var reading in recent)
            {
                _store.Alerts.Add(BuildAlert(
                    random,
                    now,
                    sensor,
                    reading.Id,
                    AlertType.ThresholdBreach,
                    $"{reading.ReadingType} reading of {reading.TextValue} on {sensor.NodeId} is outside its configured range.",
                    reading.TimestampUtc));
            }
        }

        foreach (var sensor in _store.SensorProfiles)
        {
            if (sensor.Status == SensorStatus.Offline)
            {
                _store.Alerts.Add(BuildAlert(
                    random,
                    now,
                    sensor,
                    null,
                    AlertType.DeviceOffline,
                    $"{sensor.Name} ({sensor.NodeId}) stopped reporting at {sensor.LastSeenUtc:yyyy-MM-dd HH:mm} UTC.",
                    sensor.LastSeenUtc));
            }

            if (random.NextDouble() < 0.25)
            {
                _store.Alerts.Add(BuildAlert(
                    random,
                    now,
                    sensor,
                    null,
                    AlertType.DataGap,
                    $"Gap of {random.Next(15, 180)} minutes detected in the telemetry stream for {sensor.NodeId}.",
                    now.AddHours(-random.Next(1, _options.HistoryDays * 24))));
            }

            if (random.NextDouble() < 0.18)
            {
                _store.Alerts.Add(BuildAlert(
                    random,
                    now,
                    sensor,
                    null,
                    AlertType.LowBattery,
                    $"Battery level on {sensor.NodeId} has dropped to {random.Next(3, 20)}%.",
                    now.AddHours(-random.Next(1, _options.HistoryDays * 24))));
            }
        }

        _store.Alerts.Sort((left, right) => right.TriggeredUtc.CompareTo(left.TriggeredUtc));
    }

    private static Alert BuildAlert(
        Random random,
        DateTime now,
        SensorProfile sensor,
        long? readingId,
        AlertType alertType,
        string message,
        DateTime triggeredUtc)
    {
        var status = PickStatus(random);

        return new Alert
        {
            Id = Guid.NewGuid(),
            SensorProfileId = sensor.Id,
            TelemetryReadingId = readingId,
            AlertType = alertType,
            Severity = PickSeverity(random, alertType),
            Message = message,
            TriggeredUtc = triggeredUtc,
            AcknowledgedUtc = status == AlertStatus.Active
                ? null
                : triggeredUtc.AddMinutes(random.Next(2, 240)),
            Status = status
        };
    }

    private static AlertStatus PickStatus(Random random)
    {
        var roll = random.NextDouble();
        if (roll < 0.35) return AlertStatus.Active;
        if (roll < 0.60) return AlertStatus.Acknowledged;
        return AlertStatus.Resolved;
    }

    private static AlertSeverity PickSeverity(Random random, AlertType alertType) => alertType switch
    {
        AlertType.DeviceOffline => AlertSeverity.Critical,
        AlertType.LowBattery => AlertSeverity.Warning,
        AlertType.DataGap => random.NextDouble() < 0.5 ? AlertSeverity.Info : AlertSeverity.Warning,
        _ => random.NextDouble() < 0.3 ? AlertSeverity.Critical : AlertSeverity.Warning
    };
}
