using Microsoft.AspNetCore.Mvc;

namespace BreakfastProvider.Api.Controllers;

[ApiController]
[Route("")]
[Produces("application/json")]
public class HeartbeatController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Heartbeat() => Ok(new { status = "ok" });
}
