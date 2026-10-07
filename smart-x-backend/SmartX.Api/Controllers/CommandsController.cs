using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Logic;
using SmartX.Api.Models;
using SmartX.Api.Models.Requests;
using SmartX.Api.Models.Stream;

namespace SmartX.Api.Controllers;

/// <summary>
/// The Real-Time Command Stream and History page. Every endpoint is a thin
/// call into the central <see cref="ISmartXCommandEngine"/>.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CommandsController : ControllerBase
{
    private readonly ISmartXCommandEngine _engine;

    public CommandsController(ISmartXCommandEngine engine)
    {
        _engine = engine;
    }

    /// <summary>Audit trail: the filtered command history, newest first, paged.</summary>
    [HttpGet]
    public IActionResult GetCommands(
        [FromQuery] List<CommandStatus>? statuses,
        [FromQuery] List<CommandOrigin>? origins,
        [FromQuery] List<CommandType>? commandTypes,
        [FromQuery] List<OperationCategory>? operationCategories,
        [FromQuery] List<NodeAlertState>? alertStates,
        [FromQuery] AlertSeverity? minAlertSeverity,
        [FromQuery] List<SensorCategory>? sensorCategories,
        [FromQuery] string? zone,
        [FromQuery] string? search,
        [FromQuery] bool manualOnly = false,
        [FromQuery] int windowMinutes = 60,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = BuildQuery(
            statuses, origins, commandTypes, operationCategories, alertStates,
            minAlertSeverity, sensorCategories, zone, search, manualOnly, windowMinutes);

        query.Page = page;
        query.PageSize = pageSize;

        return Ok(_engine.GetCommands(query));
    }

    /// <summary>Live tail: the newest commands in the window, for the stream panel.</summary>
    [HttpGet("stream")]
    public IActionResult GetStream(
        [FromQuery] List<CommandStatus>? statuses,
        [FromQuery] List<CommandOrigin>? origins,
        [FromQuery] List<CommandType>? commandTypes,
        [FromQuery] List<OperationCategory>? operationCategories,
        [FromQuery] List<NodeAlertState>? alertStates,
        [FromQuery] AlertSeverity? minAlertSeverity,
        [FromQuery] List<SensorCategory>? sensorCategories,
        [FromQuery] string? zone,
        [FromQuery] string? search,
        [FromQuery] bool manualOnly = false,
        [FromQuery] int windowMinutes = 60,
        [FromQuery] int take = 40)
    {
        var query = BuildQuery(
            statuses, origins, commandTypes, operationCategories, alertStates,
            minAlertSeverity, sensorCategories, zone, search, manualOnly, windowMinutes);

        return Ok(_engine.GetStream(query, take));
    }

    /// <summary>Overview: dispatch health and throughput for the same slice.</summary>
    [HttpGet("summary")]
    public IActionResult GetSummary(
        [FromQuery] List<CommandStatus>? statuses,
        [FromQuery] List<CommandOrigin>? origins,
        [FromQuery] List<CommandType>? commandTypes,
        [FromQuery] List<OperationCategory>? operationCategories,
        [FromQuery] List<NodeAlertState>? alertStates,
        [FromQuery] AlertSeverity? minAlertSeverity,
        [FromQuery] List<SensorCategory>? sensorCategories,
        [FromQuery] string? zone,
        [FromQuery] string? search,
        [FromQuery] bool manualOnly = false,
        [FromQuery] int windowMinutes = 60)
    {
        var query = BuildQuery(
            statuses, origins, commandTypes, operationCategories, alertStates,
            minAlertSeverity, sensorCategories, zone, search, manualOnly, windowMinutes);

        return Ok(_engine.GetSummary(query));
    }

    /// <summary>Values available to the filter controls and the override target picker.</summary>
    [HttpGet("filter-options")]
    public IActionResult GetFilterOptions()
    {
        return Ok(_engine.GetFilterOptions());
    }

    /// <summary>Queue a manual override against a node.</summary>
    [HttpPost]
    public IActionResult Dispatch([FromBody] DispatchCommandRequest request)
    {
        var (command, error) = _engine.Dispatch(request);

        if (command is null)
        {
            return BadRequest(new { error });
        }

        return Created($"api/commands/{command.Id}", command);
    }

    /// <summary>The undo stack: manual overrides, most recent first.</summary>
    [HttpGet("overrides")]
    public IActionResult GetOverrideHistory()
    {
        return Ok(_engine.GetOverrideHistory());
    }

    /// <summary>Undo the most recent manual override — cancel it, or send its inverse.</summary>
    [HttpPost("overrides/undo")]
    public IActionResult UndoLastOverride([FromBody] UndoOverrideRequest? request)
    {
        var (result, error) = _engine.UndoLastOverride(request?.IssuedBy, request?.ExpectedCommandId);

        if (result is null)
        {
            return Conflict(new { error });
        }

        return Ok(result);
    }

    /// <summary>
    /// Telemetry intake: routine packets join the FIFO queue, critical ones are
    /// processed before the response is sent.
    /// </summary>
    /// <summary>Re-applies the most recently undone override.</summary>
    [HttpPost("overrides/redo")]
    public IActionResult RedoLastUndo([FromBody] UndoOverrideRequest? request)
    {
        var (result, error) = _engine.RedoLastUndo(request?.IssuedBy, request?.ExpectedCommandId);

        if (result is null)
        {
            return Conflict(new { error });
        }

        return Ok(result);
    }

    [HttpPost("packets")]
    public IActionResult IngestPackets([FromBody] List<StreamPacketRequest> packets)
    {
        if (packets.Count == 0)
        {
            return BadRequest(new { error = "Send at least one packet." });
        }

        return Ok(_engine.IngestPackets(packets));
    }

    /// <summary>Queue depths, error states, disconnected nodes and recent pipeline alerts.</summary>
    [HttpGet("pipeline")]
    public IActionResult GetPipelineStatus([FromQuery] List<string>? known)
    {
        return Ok(_engine.GetPipelineStatus(known));
    }

    /// <summary>A node's log, in timestamp order, ready to chart.</summary>
    [HttpGet("nodes/{nodeId}/timeline")]
    public IActionResult GetNodeTimeline(string nodeId, [FromQuery] int minutes = 60, [FromQuery] int maxPoints = 120)
    {
        var timeline = _engine.GetNodeTimeline(nodeId, minutes, maxPoints);
        return timeline is null ? NotFound(new { error = $"No node registered with id '{nodeId}'." }) : Ok(timeline);
    }

    /// <summary>
    /// Live device panel: every registered device matching the filter — by
    /// operational category, alert state, severity, zone or free text — with
    /// its latest readings. The whole fleet is returned, worst first.
    /// </summary>
    [HttpGet("devices")]
    public IActionResult GetLiveDevices(
        [FromQuery] List<SensorCategory>? sensorCategories,
        [FromQuery] List<NodeAlertState>? alertStates,
        [FromQuery] AlertSeverity? minAlertSeverity,
        [FromQuery] string? zone,
        [FromQuery] string? search)
    {
        return Ok(_engine.GetLiveDevices(new DeviceQuery
        {
            SensorCategories = sensorCategories,
            AlertStates = alertStates,
            MinAlertSeverity = minAlertSeverity,
            Zone = zone,
            Search = search
        }));
    }

    /// <summary>Suggested actions and automated insights for the operator.</summary>
    /// <summary>Exact lookup by node id or MAC address, with the probe time.</summary>
    [HttpGet("devices/lookup")]
    public IActionResult LookupDevice([FromQuery] string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return BadRequest(new { error = "Enter a node id or MAC address." });
        }

        return Ok(_engine.LookupDevice(key));
    }

    [HttpGet("insights")]
    public IActionResult GetInsights([FromQuery] string? issuedBy)
    {
        return Ok(_engine.GetInsights(issuedBy));
    }

    /// <summary>Records a search or node selection for the action engine to learn from.</summary>
    /// <summary>Applied or dismissed feedback on one suggestion.</summary>
    [HttpPost("insights/feedback")]
    public IActionResult RecordSuggestionFeedback([FromBody] SuggestionFeedbackRequest request)
    {
        _engine.RecordSuggestionFeedback(request);
        return NoContent();
    }

    /// <summary>Clears everything the engine has learned (for demonstrating learning from scratch).</summary>
    [HttpPost("insights/reset")]
    public IActionResult ResetLearning()
    {
        _engine.ResetLearning();
        return NoContent();
    }

    [HttpPost("activity")]
    public IActionResult RecordActivity([FromBody] OperatorActivityRequest request)
    {
        _engine.RecordActivity(request);
        return NoContent();
    }

    /// <summary>
    /// The three read endpoints describe the same slice, so they take the same
    /// filter and build it the same way — the stream, the history and the
    /// summary can never end up answering slightly different questions.
    /// </summary>
    private static CommandQuery BuildQuery(
        List<CommandStatus>? statuses,
        List<CommandOrigin>? origins,
        List<CommandType>? commandTypes,
        List<OperationCategory>? operationCategories,
        List<NodeAlertState>? alertStates,
        AlertSeverity? minAlertSeverity,
        List<SensorCategory>? sensorCategories,
        string? zone,
        string? search,
        bool manualOnly,
        int windowMinutes)
    {
        return new CommandQuery
        {
            Statuses = statuses,
            Origins = origins,
            CommandTypes = commandTypes,
            OperationCategories = operationCategories,
            AlertStates = alertStates,
            MinAlertSeverity = minAlertSeverity,
            SensorCategories = sensorCategories,
            Zone = zone,
            Search = search,
            ManualOnly = manualOnly,
            WindowMinutes = windowMinutes
        };
    }
}
