using SmartX.Api.Models;

namespace SmartX.Api.Data.Seeding;

/// <summary>
/// Generates the bulk of the demo data: one time series per sensor per reading
/// type, following a daily curve with noise and occasional threshold breaches.
/// </summary>
public class TelemetryReadingSeeder : ITelemetryReadingSeeder
{
    private readonly ISmartXDataStore _store;
    private readonly SeedOptions _options;

    public TelemetryReadingSeeder(ISmartXDataStore store, SeedOptions options)
    {
        _store = store;
        _options = options;
    }

    public void Seed()
    {
        if (_store.TelemetryReadings.Count > 0)
        {
            return;
        }

        var random = new Random(_options.RandomSeed + 2);
        var now = DateTime.UtcNow;
        var start = now.AddDays(-_options.HistoryDays);
        var interval = TimeSpan.FromMinutes(_options.ReadingIntervalMinutes);

        foreach (var sensor in _store.SensorProfiles)
        {
            // An offline sensor stops reporting at the moment it was last seen.
            var seriesEnd = sensor.Status == SensorStatus.Offline ? sensor.LastSeenUtc : now;

            foreach (var readingType in ReadingTypeProfile.ForCategory(sensor.Category))
            {
                var profile = ReadingTypeProfile.For(readingType);
                var threshold = _store.SensorThresholds.FirstOrDefault(
                    t => t.SensorProfileId == sensor.Id && t.ReadingType == readingType);

                for (var timestamp = start; timestamp <= seriesEnd; timestamp += interval)
                {
                    _store.TelemetryReadings.Add(
                        BuildReading(random, sensor, profile, threshold, timestamp));
                }
            }
        }

        _store.TelemetryReadings.Sort((left, right) => left.TimestampUtc.CompareTo(right.TimestampUtc));
    }

    private TelemetryReading BuildReading(
        Random random,
        SensorProfile sensor,
        ReadingTypeProfile profile,
        SensorThreshold? threshold,
        DateTime timestamp)
    {
        var reading = new TelemetryReading
        {
            Id = _store.NextTelemetryReadingId(),
            SensorProfileId = sensor.Id,
            ReadingType = profile.ReadingType,
            Unit = profile.Unit,
            TimestampUtc = timestamp,
            Quality = PickQuality(random)
        };

        if (profile.IsBoolean)
        {
            // Motion is far more likely during working hours.
            var chance = timestamp.Hour is >= 7 and <= 18 ? 0.35 : 0.05;
            var detected = random.NextDouble() < chance;

            reading.BooleanValue = detected;
            reading.TextValue = detected ? "Detected" : "Clear";
            reading.IsAnomaly = detected && timestamp.Hour is < 5 or > 22;
            return reading;
        }

        // Daily sine curve plus noise, so charts look like real telemetry.
        var dayFraction = timestamp.TimeOfDay.TotalHours / 24.0;
        var curve = Math.Sin(dayFraction * 2 * Math.PI) * profile.DailySwing;
        var noise = (random.NextDouble() - 0.5) * profile.Noise * 2;
        var value = profile.BaseValue + curve + noise;

        var isAnomaly = random.NextDouble() < _options.AnomalyRate;
        if (isAnomaly)
        {
            var spikeUp = random.NextDouble() < 0.5;
            var span = profile.MaxThreshold - profile.MinThreshold;
            value += spikeUp ? span * 0.45 : -span * 0.45;
        }

        reading.NumericValue = Math.Round(value, 2);
        reading.TextValue = $"{reading.NumericValue:0.##} {profile.Unit}";
        reading.IsAnomaly = IsOutsideThreshold(reading.NumericValue.Value, threshold) || isAnomaly;

        return reading;
    }

    private static bool IsOutsideThreshold(double value, SensorThreshold? threshold)
    {
        if (threshold is null || !threshold.IsEnabled)
        {
            return false;
        }

        return (threshold.MinValue.HasValue && value < threshold.MinValue.Value)
            || (threshold.MaxValue.HasValue && value > threshold.MaxValue.Value);
    }

    private static ReadingQuality PickQuality(Random random)
    {
        var roll = random.NextDouble();
        if (roll < 0.95) return ReadingQuality.Good;
        if (roll < 0.99) return ReadingQuality.Suspect;
        return ReadingQuality.Bad;
    }
}
