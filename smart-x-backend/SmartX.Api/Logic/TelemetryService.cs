using SmartX.Api.Data;
using SmartX.Api.Models;
using SmartX.Api.Models.Responses;

namespace SmartX.Api.Logic;

public class TelemetryService : ITelemetryService
{
    private readonly ITelemetryRepository _telemetryRepository;

    public TelemetryService(ITelemetryRepository telemetryRepository)
    {
        _telemetryRepository = telemetryRepository;
    }

    public PagedResult<TelemetryReading> GetReadings(TelemetryQuery query)
    {
        return _telemetryRepository.Query(query);
    }

    public List<SensorSeries> GetSeries(Guid sensorProfileId, int hours, int maxPoints)
    {
        var window = Math.Clamp(hours, 1, 24 * 30);
        return _telemetryRepository.GetSeries(
            sensorProfileId,
            DateTime.UtcNow.AddHours(-window),
            maxPoints);
    }

    public EcosystemSummary GetSummary()
    {
        return _telemetryRepository.GetSummary();
    }
}
