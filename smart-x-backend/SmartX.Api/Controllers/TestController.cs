using Microsoft.AspNetCore.Mvc;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "ok",
            message = "Smart-X API is running",
            timestamp = DateTime.UtcNow
        });
    }
}
