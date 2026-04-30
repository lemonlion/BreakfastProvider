using BreakfastProvider.Api.Models.Responses;
using Microsoft.AspNetCore.Mvc;

namespace BreakfastProvider.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class EggsController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<EggsResponse> GetEggs() => new EggsResponse();
}
