using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Logic;

namespace SmartX.Api.Controllers;

/// <summary>Connectivity check. The dashboard calls it at start-up and while it waits for an offline API.</summary>
[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly ITestService _testService;

    public TestController(ITestService testService)
    {
        _testService = testService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        return Ok(await _testService.GetStatusAsync(cancellationToken));
    }
}
