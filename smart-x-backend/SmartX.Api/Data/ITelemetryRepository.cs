using SmartX.Api.Models;
using SmartX.Api.Models.Responses;

namespace SmartX.Api.Data;

public interface ITelemetryRepository
{
    PagedResult<TelemetryReading> Query(TelemetryQuery query);
    List<SensorSeries> GetSeries(Guid sensorProfileId, DateTime? fromUtc, int maxPoints);
    EcosystemSummary GetSummary();

    /// <summary>Most recent reading of one type for one sensor, or null if it has never reported.</summary>
    TelemetryReading? GetLatest(Guid sensorProfileId, ReadingType readingType);

    /// <summary>
    /// Persists a prepared block of readings together with the batch record that
    /// describes the ingest. Identity values are assigned here so callers never
    /// have to guess them.
    /// </summary>
    int AppendReadings(IReadOnlyList<TelemetryReading> readings, IngestionBatch batch);
}
