using SmartX.Api.Data;
using SmartX.Api.Models;
using SmartX.Api.Models.Responses;

namespace SmartX.Api.Logic;

public class SensorService : ISensorService
{
    private const int DetailSeriesMaxPoints = 240;

    private readonly ISensorProfileRepository _sensorProfileRepository;
    private readonly ITelemetryRepository _telemetryRepository;

    public SensorService(
        ISensorProfileRepository sensorProfileRepository,
        ITelemetryRepository telemetryRepository)
    {
        _sensorProfileRepository = sensorProfileRepository;
        _telemetryRepository = telemetryRepository;
    }

    public List<SensorListItem> GetSensors(TelemetryQuery query)
    {
        return _sensorProfileRepository.GetAll(query);
    }

    public SensorDetail? GetSensorDetail(Guid id)
    {
        var detail = _sensorProfileRepository.GetDetail(id);
        if (detail is null)
        {
            return null;
        }

        detail.Series = _telemetryRepository.GetSeries(
            id,
            DateTime.UtcNow.AddHours(-24),
            DetailSeriesMaxPoints);

        return detail;
    }

    public FilterOptions GetFilterOptions()
    {
        return _sensorProfileRepository.GetFilterOptions();
    }
}
