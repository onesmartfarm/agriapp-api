using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriApp.Api.Controllers;

[ApiController]
[Route("api")]
public class HealthController : ControllerBase
{
    [HttpGet("healthz")]
    [AllowAnonymous]
    public IActionResult HealthCheck()
    {
        return Ok(new { status = "ok" });
    }
}
