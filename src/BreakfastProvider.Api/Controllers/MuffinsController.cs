using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BreakfastProvider.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
public class MuffinsController(IMuffinService muffinService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MuffinResponse>> MakeMuffins([FromBody] MuffinRequest request, CancellationToken cancellationToken)
    {
        var response = await muffinService.MakeMuffinsAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }
}
