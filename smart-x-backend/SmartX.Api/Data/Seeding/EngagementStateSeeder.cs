using SmartX.Api.Models;

namespace SmartX.Api.Data.Seeding;

/// <summary>
/// Seeds the engagement scoreboard. The first user reflects the real totals in
/// the store so the demo account's figures match the rest of the dashboard.
/// </summary>
public class EngagementStateSeeder : IEngagementStateSeeder
{
    private static readonly string[] UserIds =
    [
        "troy.kruger", "s.naidoo", "m.botha", "l.dlamini",
        "field.tech.01", "field.tech.02", "ops.lead", "night.shift"
    ];

    private readonly ISmartXDataStore _store;
    private readonly SeedOptions _options;

    public EngagementStateSeeder(ISmartXDataStore store, SeedOptions options)
    {
        _store = store;
        _options = options;
    }

    public void Seed()
    {
        if (_store.EngagementStates.Count > 0)
        {
            return;
        }

        var random = new Random(_options.RandomSeed + 6);
        var now = DateTime.UtcNow;

        var totalSensors = _store.SensorProfiles.Count;
        var totalReadings = _store.TelemetryReadings.Count;
        var totalAttachments = _store.SensorAttachments.Count;
        var resolvedAlerts = _store.Alerts.Count(alert => alert.Status == AlertStatus.Resolved);
        var meshHealth = CalculateMeshHealthScore();

        var userCount = Math.Min(_options.EngagementUserCount, UserIds.Length);

        for (var i = 0; i < userCount; i++)
        {
            var isPrimary = i == 0;
            var share = isPrimary ? 1.0 : 0.15 + random.NextDouble() * 0.5;

            _store.EngagementStates.Add(new EngagementState
            {
                Id = Guid.NewGuid(),
                UserId = UserIds[i],
                SensorsRegistered = (int)(totalSensors * share),
                ReadingsIngested = (int)(totalReadings * share),
                AlertsResolved = (int)(resolvedAlerts * share),
                AttachmentsUploaded = (int)(totalAttachments * share),
                MeshHealthScore = isPrimary
                    ? meshHealth
                    : Math.Round(Math.Clamp(meshHealth + (random.NextDouble() - 0.5) * 20, 0, 100), 1),
                LastUpdatedUtc = isPrimary ? now : now.AddMinutes(-random.Next(5, 4_320))
            });
        }
    }

    private double CalculateMeshHealthScore()
    {
        if (_store.SensorProfiles.Count == 0)
        {
            return 0;
        }

        var online = _store.SensorProfiles.Count(sensor => sensor.Status == SensorStatus.Online);
        var warning = _store.SensorProfiles.Count(sensor => sensor.Status == SensorStatus.Warning);
        var weighted = online + warning * 0.5;

        return Math.Round(weighted / _store.SensorProfiles.Count * 100, 1);
    }
}
