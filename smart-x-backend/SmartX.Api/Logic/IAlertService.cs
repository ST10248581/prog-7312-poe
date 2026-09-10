using SmartX.Api.Models;

namespace SmartX.Api.Logic;

public interface IAlertService
{
    List<Alert> GetAlerts(AlertStatus? status, int take);
}
