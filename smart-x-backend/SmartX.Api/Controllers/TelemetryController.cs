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
    public IActionResult GetSummary()
    {
        return Ok(_telemetryService.GetSummary());
    }

    /// <summary>The live chart for one sensor, with its threshold markers.</summary>
    [HttpGet("series/{sensorProfileId:guid}")]
    public IActionResult GetSeries(
        Guid sensorProfileId,
        [FromQuery] int hours = 24,
        [FromQuery] int maxPoints = DefaultSeriesMaxPoints)
    {
        return Ok(_telemetryService.GetSeries(sensorProfileId, hours, maxPoints));
    }

    /// <summary>Paged raw readings, for the investigate step.</summary>
    [HttpGet("readings")]
    public IActionResult GetReadings(
        [FromQuery] List<Guid>? sensorProfileIds,
        [FromQuery] List<SensorCategory>? categories,
        [FromQuery] List<SensorStatus>? statuses,
        [FromQuery] List<string>? zones,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
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

        return Ok(_telemetryService.GetReadings(query));
    }
}
