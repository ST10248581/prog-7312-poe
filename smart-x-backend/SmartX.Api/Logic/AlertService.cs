using SmartX.Api.Data;
using SmartX.Api.Models;

namespace SmartX.Api.Logic;

public class AlertService : IAlertService
{
    private readonly IAlertRepository _alertRepository;

    public AlertService(IAlertRepository alertRepository)
    {
        _alertRepository = alertRepository;
    }

    public List<Alert> GetAlerts(AlertStatus? status, int take)
    {
        return _alertRepository.GetPrioritised(status, take);
    }
}
