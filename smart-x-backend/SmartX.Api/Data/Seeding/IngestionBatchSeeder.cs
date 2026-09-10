using SmartX.Api.Models;

namespace SmartX.Api.Data.Seeding;

/// <summary>
/// One batch per sensor per interval, describing how the readings for that
/// window arrived at the ingestion API.
/// </summary>
public class IngestionBatchSeeder : IIngestionBatchSeeder
{
    private readonly ISmartXDataStore _store;
    private readonly SeedOptions _options;

    public IngestionBatchSeeder(ISmartXDataStore store, SeedOptions options)
    {
        _store = store;
        _options = options;
    }

    public void Seed()
    {
        if (_store.IngestionBatches.Count > 0)
        {
            return;
        }

        var random = new Random(_options.RandomSeed + 5);
        var now = DateTime.UtcNow;
        var start = now.AddDays(-_options.HistoryDays);
        var interval = TimeSpan.FromMinutes(_options.BatchIntervalMinutes);

        var readingsPerBatch = Math.Max(
            1,
            _options.BatchIntervalMinutes / Math.Max(1, _options.ReadingIntervalMinutes));

        foreach (var sensor in _store.SensorProfiles)
        {
            var seriesCount = ReadingTypeProfile.ForCategory(sensor.Category).Length;
            var expected = readingsPerBatch * seriesCount;
            var lastBatch = sensor.Status == SensorStatus.Offline ? sensor.LastSeenUtc : now;
            var sourceIp = $"10.{random.Next(0, 6)}.{random.Next(0, 256)}.{random.Next(2, 254)}";

            for (var received = start; received <= lastBatch; received += interval)
            {
                var readingCount = expected + random.Next(-1, 2);
                if (readingCount < 1)
                {
                    readingCount = 1;
                }

                var rejected = random.NextDouble() < 0.12 ? random.Next(1, Math.Max(2, readingCount / 4)) : 0;

                _store.IngestionBatches.Add(new IngestionBatch
                {
                    Id = Guid.NewGuid(),
                    SensorProfileId = sensor.Id,
                    ReceivedUtc = received,
                    ReadingCount = readingCount,
                    AcceptedCount = readingCount - rejected,
                    RejectedCount = rejected,
                    SourceIpAddress = sourceIp,
                    ProcessingMs = random.Next(4, 180)
                });
            }
        }

        _store.IngestionBatches.Sort((left, right) => right.ReceivedUtc.CompareTo(left.ReceivedUtc));
    }
}
