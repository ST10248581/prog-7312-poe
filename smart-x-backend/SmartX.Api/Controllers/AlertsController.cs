using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Logic;
using SmartX.Api.Models;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlertsController : ControllerBase
{
    private readonly IAlertService _alertService;

    public AlertsController(IAlertService alertService)
    {
        _alertService = alertService;
    }

    /// <summary>Severity-ordered and capped, so the feed stays actionable.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAlerts([FromQuery] AlertStatus? status, CancellationToken cancellationToken, [FromQuery] int take = 25)
    {
        return Ok(await _alertService.GetAlertsAsync(status, take, cancellationToken));
    }
}
