using SmartX.Api.Models;
using SmartX.Api.Models.Responses;

namespace SmartX.Api.Data;

public interface ITelemetryRepository
{
    PagedResult<TelemetryReading> Query(TelemetryQuery query);
    List<SensorSeries> GetSeries(Guid sensorProfileId, DateTime? fromUtc, int maxPoints);
    EcosystemSummary GetSummary();
}
