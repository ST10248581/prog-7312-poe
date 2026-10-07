using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Logic;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EngagementController : ControllerBase
{
    private readonly IEngagementService _engagementService;

    public EngagementController(IEngagementService engagementService)
    {
        _engagementService = engagementService;
    }

    /// <summary>Configuration-progress state for the engagement strip.</summary>
    [HttpGet]
    public async Task<IActionResult> GetEngagement([FromQuery] string? userId, CancellationToken cancellationToken)
    {
        var state = await _engagementService.GetEngagementAsync(userId, cancellationToken);
        return state is null ? NotFound() : Ok(state);
    }
}
