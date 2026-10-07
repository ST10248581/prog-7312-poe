using SmartX.Api.Models;

namespace SmartX.Api.Logic;

public interface IAlertService
{
    Task<List<Alert>> GetAlertsAsync(AlertStatus? status, int take, CancellationToken cancellationToken = default);
}
