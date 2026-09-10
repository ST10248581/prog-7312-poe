using SmartX.Api.Models;

namespace SmartX.Api.Data;

public class AlertRepository : IAlertRepository
{
    private readonly ISmartXDataStore _store;

    public AlertRepository(ISmartXDataStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Alerts are returned severity-first and capped. Returning everything with
    /// equal weight is what produces alert fatigue, so the API itself does the
    /// prioritising rather than leaving it to the dashboard.
    /// </summary>
    public List<Alert> GetPrioritised(AlertStatus? status, int take)
    {
        var alerts = _store.Alerts.AsEnumerable();

        if (status.HasValue)
        {
            alerts = alerts.Where(alert => alert.Status == status.Value);
        }

        return alerts
            .OrderByDescending(alert => alert.Severity)
            .ThenByDescending(alert => alert.TriggeredUtc)
            .Take(Math.Clamp(take, 1, 200))
            .ToList();
    }
}
