using SmartX.Api.Models;
using SmartX.Api.Models.Responses;

namespace SmartX.Api.Logic;

public interface ITelemetryService
{
    Task<PagedResult<TelemetryReading>> GetReadingsAsync(TelemetryQuery query, CancellationToken cancellationToken = default);
    Task<List<SensorSeries>> GetSeriesAsync(Guid sensorProfileId, int hours, int maxPoints, CancellationToken cancellationToken = default);
    Task<EcosystemSummary> GetSummaryAsync(CancellationToken cancellationToken = default);
}
