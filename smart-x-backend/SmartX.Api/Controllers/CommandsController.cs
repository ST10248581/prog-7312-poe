using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Logic;
using SmartX.Api.Models;
using SmartX.Api.Models.Requests;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommandsController : ControllerBase
{
    private readonly ICommandService _commandService;

    public CommandsController(ICommandService commandService)
    {
        _commandService = commandService;
    }

    /// <summary>Audit trail: the filtered command history, newest first, paged.</summary>
    [HttpGet]
    public IActionResult GetCommands(
        [FromQuery] List<CommandStatus>? statuses,
        [FromQuery] List<CommandOrigin>? origins,
        [FromQuery] List<CommandType>? commandTypes,
        [FromQuery] string? zone,
        [FromQuery] string? search,
        [FromQuery] bool manualOnly = false,
        [FromQuery] int windowMinutes = 60,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = BuildQuery(statuses, origins, commandTypes, zone, search, manualOnly, windowMinutes);
        query.Page = page;
        query.PageSize = pageSize;

        return Ok(_commandService.GetCommands(query));
    }

    /// <summary>Live tail: the newest commands in the window, for the stream panel.</summary>
    [HttpGet("stream")]
    public IActionResult GetStream(
        [FromQuery] List<CommandStatus>? statuses,
        [FromQuery] List<CommandOrigin>? origins,
        [FromQuery] List<CommandType>? commandTypes,
        [FromQuery] string? zone,
        [FromQuery] string? search,
        [FromQuery] bool manualOnly = false,
        [FromQuery] int windowMinutes = 60,
        [FromQuery] int take = 40)
    {
        var query = BuildQuery(statuses, origins, commandTypes, zone, search, manualOnly, windowMinutes);
        return Ok(_commandService.GetStream(query, take));
    }

    /// <summary>Overview: dispatch health and throughput for the same slice.</summary>
    [HttpGet("summary")]
    public IActionResult GetSummary(
        [FromQuery] List<CommandStatus>? statuses,
        [FromQuery] List<CommandOrigin>? origins,
        [FromQuery] List<CommandType>? commandTypes,
        [FromQuery] string? zone,
        [FromQuery] string? search,
        [FromQuery] bool manualOnly = false,
        [FromQuery] int windowMinutes = 60)
    {
        var query = BuildQuery(statuses, origins, commandTypes, zone, search, manualOnly, windowMinutes);
        return Ok(_commandService.GetSummary(query));
    }

    /// <summary>Values available to the filter controls and the override target picker.</summary>
    [HttpGet("filter-options")]
    public IActionResult GetFilterOptions()
    {
        return Ok(_commandService.GetFilterOptions());
    }

    /// <summary>Queue a manual override against a node.</summary>
    [HttpPost]
    public IActionResult Dispatch([FromBody] DispatchCommandRequest request)
    {
        var (command, error) = _commandService.Dispatch(request);

        if (command is null)
        {
            return BadRequest(new { error });
        }

        return Created($"api/commands/{command.Id}", command);
    }

    private static CommandQuery BuildQuery(
        List<CommandStatus>? statuses,
        List<CommandOrigin>? origins,
        List<CommandType>? commandTypes,
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
            Zone = zone,
            Search = search,
            ManualOnly = manualOnly,
            WindowMinutes = windowMinutes
        };
    }
}
