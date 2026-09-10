using SmartX.Api.Models;

namespace SmartX.Api.Data;

public interface IAlertRepository
{
    List<Alert> GetPrioritised(AlertStatus? status, int take);
}
