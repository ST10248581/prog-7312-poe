using SmartX.Api.Models;

namespace SmartX.Api.Data.Seeding;

public class SensorThresholdSeeder : ISensorThresholdSeeder
{
    private readonly ISmartXDataStore _store;
    private readonly SeedOptions _options;

    public SensorThresholdSeeder(ISmartXDataStore store, SeedOptions options)
    {
        _store = store;
        _options = options;
    }

    public void Seed()
    {
        if (_store.SensorThresholds.Count > 0)
        {
            return;
        }

        var random = new Random(_options.RandomSeed + 1);

        foreach (var sensor in _store.SensorProfiles)
        {
            foreach (var readingType in ReadingTypeProfile.ForCategory(sensor.Category))
            {
                var profile = ReadingTypeProfile.For(readingType);

                // Nudge each sensor's limits slightly so the data set is not uniform.
                var drift = profile.IsBoolean ? 0 : (random.NextDouble() - 0.5) * profile.Noise * 2;

                _store.SensorThresholds.Add(new SensorThreshold
                {
                    Id = Guid.NewGuid(),
                    SensorProfileId = sensor.Id,
                    ReadingType = readingType,
                    MinValue = profile.IsBoolean ? null : Math.Round(profile.MinThreshold + drift, 2),
                    MaxValue = profile.IsBoolean ? null : Math.Round(profile.MaxThreshold + drift, 2),
                    Severity = PickSeverity(random),
                    IsEnabled = random.NextDouble() < 0.9
                });
            }
        }
    }

    private static AlertSeverity PickSeverity(Random random)
    {
        var roll = random.NextDouble();
        if (roll < 0.25) return AlertSeverity.Info;
        if (roll < 0.75) return AlertSeverity.Warning;
        return AlertSeverity.Critical;
    }
}
