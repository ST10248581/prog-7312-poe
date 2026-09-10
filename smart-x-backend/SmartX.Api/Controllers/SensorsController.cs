using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Logic;
using SmartX.Api.Models;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensorsController : ControllerBase
{
    private readonly ISensorService _sensorService;

    public SensorsController(ISensorService sensorService)
    {
        _sensorService = sensorService;
    }

    /// <summary>Overview + filter: the sensor grid, narrowed by the active filters.</summary>
    [HttpGet]
    public IActionResult GetSensors(
        [FromQuery] List<SensorCategory>? categories,
        [FromQuery] List<SensorStatus>? statuses,
        [FromQuery] List<string>? zones,
        [FromQuery] bool anomaliesOnly = false)
    {
        var query = new TelemetryQuery
        {
            Categories = categories,
            Statuses = statuses,
            Zones = zones,
            AnomaliesOnly = anomaliesOnly
        };

        return Ok(_sensorService.GetSensors(query));
    }

    /// <summary>Details on demand: everything known about one sensor.</summary>
    [HttpGet("{id:guid}")]
    public IActionResult GetSensor(Guid id)
    {
        var detail = _sensorService.GetSensorDetail(id);
        return detail is null ? NotFound() : Ok(detail);
    }

    /// <summary>Values available to the filter controls.</summary>
    [HttpGet("filter-options")]
    public IActionResult GetFilterOptions()
    {
        return Ok(_sensorService.GetFilterOptions());
    }
}
