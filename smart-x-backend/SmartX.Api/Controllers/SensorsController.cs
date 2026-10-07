using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Logic;
using SmartX.Api.Models;
using SmartX.Api.Models.Requests;

namespace SmartX.Api.Controllers;

/// <summary>
/// Sensor registry, registration edits and attachments. Request models are
/// validated by [ApiController] before an action runs; a failure returns a 400
/// ValidationProblemDetails whose "errors" map names each field, so the
/// dashboard can show the message beside the input that caused it.
/// </summary>
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
    public async Task<IActionResult> GetSensors(
        [FromQuery] List<SensorCategory>? categories,
        [FromQuery] List<SensorStatus>? statuses,
        [FromQuery] List<string>? zones,
        CancellationToken cancellationToken,
        [FromQuery] bool anomaliesOnly = false)
    {
        var query = new TelemetryQuery
        {
            Categories = categories,
            Statuses = statuses,
            Zones = zones,
            AnomaliesOnly = anomaliesOnly
        };

        return Ok(await _sensorService.GetSensorsAsync(query, cancellationToken));
    }

    /// <summary>Details on demand: everything known about one sensor.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetSensor(Guid id, CancellationToken cancellationToken)
    {
        var detail = await _sensorService.GetSensorDetailAsync(id, cancellationToken);
        return detail is null ? NotFound() : Ok(detail);
    }

    /// <summary>Values available to the filter controls.</summary>
    [HttpGet("filter-options")]
    public async Task<IActionResult> GetFilterOptions(CancellationToken cancellationToken)
    {
        return Ok(await _sensorService.GetFilterOptionsAsync(cancellationToken));
    }

    /// <summary>Register a new sensor device.</summary>
    /// <response code="201">The device was registered.</response>
    /// <response code="400">A field failed validation.</response>
    /// <response code="409">The MAC address or node id is already registered.</response>
    [HttpPost]
    public async Task<IActionResult> CreateSensor([FromBody] CreateSensorRequest request, CancellationToken cancellationToken)
    {
        var result = await _sensorService.CreateSensorAsync(request, cancellationToken);

        return result.Status switch
        {
            WriteStatus.Success => CreatedAtAction(nameof(GetSensor), new { id = result.Value!.Id }, result.Value),
            WriteStatus.Conflict => FieldConflict(result.Field!, result.Error!),
            _ => FieldInvalid(result.Field ?? string.Empty, result.Error ?? "The device could not be registered.")
        };
    }

    /// <summary>Update the sensor payload / registration fields.</summary>
    [HttpPut("{id:guid}/payload")]
    public async Task<IActionResult> UpdatePayload(Guid id, [FromBody] UpdateSensorPayloadRequest request, CancellationToken cancellationToken)
    {
        var result = await _sensorService.UpdateSensorPayloadAsync(id, request, cancellationToken);

        return result.Status switch
        {
            WriteStatus.Success => Ok(result.Value),
            WriteStatus.NotFound => NotFound(),
            WriteStatus.Conflict => FieldConflict(result.Field!, result.Error!),
            _ => FieldInvalid(result.Field ?? string.Empty, result.Error ?? "The registration could not be saved.")
        };
    }

    /// <summary>
    /// Upload a file attachment to a sensor profile. The multipart body size is
    /// capped globally from Attachments:MaxFileSizeBytes (see Program.cs); the
    /// file is then checked against the allow-list, hashed and encrypted.
    /// </summary>
    [HttpPost("{sensorId:guid}/attachments")]
    public async Task<IActionResult> UploadAttachment(
        Guid sensorId,
        [FromForm] IFormFile file,
        [FromForm] AttachmentType attachmentType,
        CancellationToken cancellationToken,
        [FromForm] string description = "")
    {
        var result = await _sensorService.UploadAttachmentAsync(sensorId, file, attachmentType, description, cancellationToken);

        return result.Status switch
        {
            WriteStatus.Success => Created($"api/sensors/{sensorId}/attachments/{result.Value!.Id}", result.Value),
            WriteStatus.NotFound => NotFound(),
            _ => FieldInvalid(result.Field ?? "file", result.Error ?? "The file was rejected.")
        };
    }

    /// <summary>Download an attachment, decrypted and integrity-checked.</summary>
    [HttpGet("{sensorId:guid}/attachments/{attachmentId:guid}/download")]
    public async Task<IActionResult> DownloadAttachment(Guid sensorId, Guid attachmentId, CancellationToken cancellationToken)
    {
        try
        {
            var download = await _sensorService.DownloadAttachmentAsync(sensorId, attachmentId, cancellationToken);
            if (download is null)
            {
                return NotFound();
            }

            // FileStreamResult disposes the stream once the response is written.
            return File(download.Content, download.ContentType, download.Attachment.FileName);
        }
        catch (InvalidDataException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Attachment integrity check failed",
                detail: exception.Message);
        }
    }

    /// <summary>400 in the same shape as automatic model validation, for checks only the service can make.</summary>
    private IActionResult FieldInvalid(string field, string message)
    {
        ModelState.AddModelError(field, message);
        return ValidationProblem(ModelState);
    }

    /// <summary>409 carrying the clashing field, so the client can mark the right input.</summary>
    private IActionResult FieldConflict(string field, string message)
    {
        var problem = new ValidationProblemDetails(new Dictionary<string, string[]> { [field] = [message] })
        {
            Status = StatusCodes.Status409Conflict,
            Title = "This device clashes with one already registered."
        };

        return Conflict(problem);
    }
}
