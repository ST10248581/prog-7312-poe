using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Logic;
using SmartX.Api.Models;
using SmartX.Api.Models.Requests;

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

    /// <summary>Register a new sensor device.</summary>
    [HttpPost]
    public IActionResult CreateSensor([FromBody] CreateSensorRequest request)
    {
        var sensor = _sensorService.CreateSensor(request);
        return CreatedAtAction(nameof(GetSensor), new { id = sensor.Id }, sensor);
    }

    /// <summary>Update the sensor payload / registration fields.</summary>
    [HttpPut("{id:guid}/payload")]
    public IActionResult UpdatePayload(Guid id, [FromBody] UpdateSensorPayloadRequest request)
    {
        var updated = _sensorService.UpdateSensorPayload(id, request);
        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>Upload a file attachment to a sensor profile.</summary>
    [HttpPost("{sensorId:guid}/attachments")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public IActionResult UploadAttachment(
        Guid sensorId,
        [FromForm] IFormFile file,
        [FromForm] AttachmentType attachmentType,
        [FromForm] string description = "")
    {
        var attachment = _sensorService.UploadAttachment(sensorId, file, attachmentType, description);
        return attachment is null ? NotFound() : Created($"api/sensors/{sensorId}/attachments/{attachment.Id}", attachment);
    }

    /// <summary>Download an attachment file.</summary>
    [HttpGet("{sensorId:guid}/attachments/{attachmentId:guid}/download")]
    public IActionResult DownloadAttachment(Guid sensorId, Guid attachmentId)
    {
        var result = _sensorService.DownloadAttachment(sensorId, attachmentId);
        if (result is null)
        {
            return NotFound();
        }

        var (attachment, fileData) = result.Value;
        return File(fileData, attachment.ContentType, attachment.FileName);
    }
}
