using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Logic;
using SmartX.Api.Models;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelemetryController : ControllerBase
{
    private const int DefaultSeriesMaxPoints = 180;

    private readonly ITelemetryService _telemetryService;

    public TelemetryController(ITelemetryService telemetryService)
    {
        _telemetryService = telemetryService;
    }

    /// <summary>Overview first: whole-ecosystem health in a single payload.</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        return Ok(await _telemetryService.GetSummaryAsync(cancellationToken));
    }

    /// <summary>The live chart for one sensor, with its threshold markers.</summary>
    [HttpGet("series/{sensorProfileId:guid}")]
    public async Task<IActionResult> GetSeries(
        Guid sensorProfileId,
        CancellationToken cancellationToken,
        [FromQuery] int hours = 24,
        [FromQuery] int maxPoints = DefaultSeriesMaxPoints)
    {
        return Ok(await _telemetryService.GetSeriesAsync(sensorProfileId, hours, maxPoints, cancellationToken));
    }

    /// <summary>Paged raw readings, for the investigate step.</summary>
    [HttpGet("readings")]
    public async Task<IActionResult> GetReadings(
        [FromQuery] List<Guid>? sensorProfileIds,
        [FromQuery] List<SensorCategory>? categories,
        [FromQuery] List<SensorStatus>? statuses,
        [FromQuery] List<string>? zones,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken cancellationToken,
        [FromQuery] bool anomaliesOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = new TelemetryQuery
        {
            SensorProfileIds = sensorProfileIds,
            Categories = categories,
            Statuses = statuses,
            Zones = zones,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            AnomaliesOnly = anomaliesOnly,
            Page = page,
            PageSize = pageSize
        };

        return Ok(await _telemetryService.GetReadingsAsync(query, cancellationToken));
    }
}
