using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Logic;
using SmartX.Api.Models;
using SmartX.Api.Models.Topology;

namespace SmartX.Api.Controllers;

/// <summary>
/// The Network Topology and Mesh Routing page. Every endpoint is a thin call
/// into the central <see cref="ISmartXTopologyEngine"/>.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TopologyController : ControllerBase
{
    private readonly ISmartXTopologyEngine _engine;

    public TopologyController(ISmartXTopologyEngine engine)
    {
        _engine = engine;
    }

    /// <summary>The live mesh: nodes, weighted links, the spanning tree and the routing tree.</summary>
    [HttpGet]
    public IActionResult GetTopology()
    {
        return Ok(_engine.GetTopology());
    }

    /// <summary>Rebuilds the graph from the registry and recomputes every route.</summary>
    [HttpPost("recompute")]
    public IActionResult Recompute()
    {
        return Ok(_engine.Recompute());
    }

    /// <summary>Traces a node's path to the gateway by MAC address (any notation) or node id.</summary>
    [HttpGet("routes")]
    public IActionResult TraceRoute([FromQuery] string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return BadRequest(new { error = "Supply a MAC address or node id as ?key=." });
        }

        var trace = _engine.TraceRoute(key);
        return trace.Found ? Ok(trace) : NotFound(trace);
    }

    /// <summary>Takes a node off the mesh to watch the routes heal around it.</summary>
    [HttpPost("nodes/{key}/fail")]
    public IActionResult FailNode(string key)
    {
        var result = _engine.SetSimulatedFailure(key, failed: true);
        return result is null ? NotFound(new { error = $"No node or gateway matches \"{key}\"." }) : Ok(result);
    }

    /// <summary>Brings a node taken down by <see cref="FailNode"/> back.</summary>
    [HttpPost("nodes/{key}/restore")]
    public IActionResult RestoreNode(string key)
    {
        var result = _engine.SetSimulatedFailure(key, failed: false);
        return result is null ? NotFound(new { error = $"No node or gateway matches \"{key}\"." }) : Ok(result);
    }

    /// <summary>Clears every simulated outage.</summary>
    [HttpPost("failures/clear")]
    public IActionResult ClearFailures()
    {
        return Ok(_engine.ClearSimulatedFailures());
    }

    /// <summary>
    /// BFS or DFS from a gateway over the live mesh: every device connected to it
    /// (optionally within one subnet / zone), which are down, and where to hop.
    /// </summary>
    [HttpGet("scan")]
    public IActionResult Scan(
        [FromQuery] string? root,
        [FromQuery] string? zone,
        [FromQuery] TraversalAlgorithm algorithm = TraversalAlgorithm.Bfs)
    {
        var scan = _engine.ScanConnectivity(string.IsNullOrWhiteSpace(root) ? "ALL" : root, zone, algorithm);
        return scan is null
            ? NotFound(new { error = $"\"{root}\" is not a gateway. Use a gateway id (e.g. GW-01) or ALL." })
            : Ok(scan);
    }

    /// <summary>
    /// A sensor's telemetry history, ordered and paged, with the route each
    /// reading took. Pass <c>from</c> and <c>to</c> (ISO 8601) for an exact range
    /// such as 14:00–16:00, or <c>hours</c> for the last N hours.
    /// </summary>
    [HttpGet("history")]
    public IActionResult GetHistory(
        [FromQuery] string? key,
        [FromQuery] ReadingType? readingType,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int hours = 24,
        [FromQuery] HistorySort sort = HistorySort.Newest,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return BadRequest(new { error = "Supply a sensor's MAC address or node id as ?key=." });
        }

        if (from.HasValue != to.HasValue)
        {
            return BadRequest(new { error = "Supply both ?from= and ?to=, or neither." });
        }

        if (from.HasValue && from.Value.ToUniversalTime() >= to!.Value.ToUniversalTime())
        {
            return BadRequest(new { error = "?from= must be earlier than ?to=." });
        }

        var history = _engine.GetTelemetryHistory(key, hours, sort, readingType, page, pageSize, from, to);
        return history is null
            ? NotFound(new { error = $"No sensor matches \"{key}\". Gateways relay telemetry but do not report any." })
            : Ok(history);
    }
}
