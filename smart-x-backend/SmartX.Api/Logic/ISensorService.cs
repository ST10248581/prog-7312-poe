using SmartX.Api.Models;
using SmartX.Api.Models.Responses;

namespace SmartX.Api.Logic;

public interface ISensorService
{
    List<SensorListItem> GetSensors(TelemetryQuery query);
    SensorDetail? GetSensorDetail(Guid id);
    FilterOptions GetFilterOptions();
}
