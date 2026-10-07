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
    public async Task<IActionResult> Ingest(Guid sensorId, [FromBody] IngestTelemetryRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SourceIpAddress))
        {
            request.SourceIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
        }

        var result = await _engine.IngestHistoricalBatchesAsync(sensorId, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Aggregate draw of the selected meters.</summary>
    [HttpGet("load")]
    public async Task<IActionResult> GetAggregateLoad([FromQuery] List<Guid> sensorIds, CancellationToken cancellationToken)
    {
        if (sensorIds.Count == 0)
        {
            return BadRequest("Supply at least one sensorIds value.");
        }

        return Ok(await _engine.GetAggregateLoadAsync(sensorIds, cancellationToken));
    }

    /// <summary>Aggregate draw of every meter in one zone.</summary>
    [HttpGet("load/zone/{zone}")]
    public async Task<IActionResult> GetZoneLoad(string zone, CancellationToken cancellationToken)
    {
        return Ok(await _engine.GetZoneLoadAsync(zone, cancellationToken));
    }

    /// <summary>Delta between two meters.</summary>
    [HttpGet("load/compare")]
    public async Task<IActionResult> CompareLoad([FromQuery] Guid left, [FromQuery] Guid right, CancellationToken cancellationToken)
    {
        try
        {
            var comparison = await _engine.CompareLoadAsync(left, right, cancellationToken);
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
    public async Task<IActionResult> ValidateDeployment([FromQuery] string? zone, CancellationToken cancellationToken)
    {
        return Ok(await _engine.ValidateDeploymentAsync(zone, cancellationToken));
    }

    /// <summary>Validates a proposed configuration profile before it is rolled out.</summary>
    [HttpPost("deployment/validate")]
    public async Task<IActionResult> ValidateDeployment([FromBody] DeploymentNode root, CancellationToken cancellationToken)
    {
        return Ok(await _engine.ValidateDeploymentAsync(root, cancellationToken));
    }
}
