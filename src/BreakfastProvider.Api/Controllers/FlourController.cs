using BreakfastProvider.Api.Models.Responses;
using Microsoft.AspNetCore.Mvc;

namespace BreakfastProvider.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class FlourController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<FlourResponse> GetFlour() => new FlourResponse();
}
