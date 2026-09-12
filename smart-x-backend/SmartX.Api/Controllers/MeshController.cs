using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Logic;
using SmartX.Api.Models.Requests;
using SmartX.Api.Models.Telemetry;

namespace SmartX.Api.Controllers;

/// <summary>
/// Mesh-level operations that span more than one sensor: gateway ingestion,
/// aggregate load arithmetic and deployment validation. All of it is served by
/// the central <see cref="ISmartXTelemetryEngine"/>.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MeshController : ControllerBase
{
    private readonly ISmartXTelemetryEngine _engine;

    public MeshController(ISmartXTelemetryEngine engine)
    {
        _engine = engine;
    }

    /// <summary>Accepts a gateway buffer flush: sequential batches of raw samples.</summary>
    [HttpPost("sensors/{sensorId:guid}/ingest")]
    public IActionResult Ingest(Guid sensorId, [FromBody] IngestTelemetryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SourceIpAddress))
        {
            request.SourceIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
        }

        var result = _engine.IngestHistoricalBatches(sensorId, request);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Aggregate draw of the selected meters.</summary>
    [HttpGet("load")]
    public IActionResult GetAggregateLoad([FromQuery] List<Guid> sensorIds)
    {
        if (sensorIds.Count == 0)
        {
            return BadRequest("Supply at least one sensorIds value.");
        }

        return Ok(_engine.GetAggregateLoad(sensorIds));
    }

    /// <summary>Aggregate draw of every meter in one zone.</summary>
    [HttpGet("load/zone/{zone}")]
    public IActionResult GetZoneLoad(string zone)
    {
        return Ok(_engine.GetZoneLoad(zone));
    }

    /// <summary>Delta between two meters.</summary>
    [HttpGet("load/compare")]
    public IActionResult CompareLoad([FromQuery] Guid left, [FromQuery] Guid right)
    {
        try
        {
            var comparison = _engine.CompareLoad(left, right);
            return comparison is null ? NotFound() : Ok(comparison);
        }
        catch (InvalidOperationException exception)
        {
            // Raised when the two meters report in incompatible units.
            return BadRequest(exception.Message);
        }
    }

    /// <summary>Validates the deployment tree the mesh is currently registered as.</summary>
    [HttpGet("deployment")]
    public IActionResult ValidateDeployment([FromQuery] string? zone)
    {
        return Ok(_engine.ValidateDeployment(zone));
    }

    /// <summary>Validates a proposed configuration profile before it is rolled out.</summary>
    [HttpPost("deployment/validate")]
    public IActionResult ValidateDeployment([FromBody] DeploymentNode root)
    {
        return Ok(_engine.ValidateDeployment(root));
    }
}
