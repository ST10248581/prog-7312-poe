using SmartX.Api.Models;
using SmartX.Api.Models.Responses;

namespace SmartX.Api.Logic;

public interface ITelemetryService
{
    PagedResult<TelemetryReading> GetReadings(TelemetryQuery query);
    List<SensorSeries> GetSeries(Guid sensorProfileId, int hours, int maxPoints);
    EcosystemSummary GetSummary();
}
