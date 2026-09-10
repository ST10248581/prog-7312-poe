using SmartX.Api.Models;
using SmartX.Api.Models.Responses;

namespace SmartX.Api.Data;

public interface ISensorProfileRepository
{
    List<SensorListItem> GetAll(TelemetryQuery query);
    SensorDetail? GetDetail(Guid id);
    FilterOptions GetFilterOptions();
}
