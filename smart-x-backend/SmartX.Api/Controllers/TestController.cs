using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Logic;

namespace SmartX.Api.Controllers;

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
    public IActionResult Get()
    {
        var result = _testService.GetStatus();
        return Ok(result);
    }
}
