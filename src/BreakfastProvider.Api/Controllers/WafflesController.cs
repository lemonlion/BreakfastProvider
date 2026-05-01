using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BreakfastProvider.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
public class WafflesController(IWaffleService waffleService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WaffleResponse>> MakeWaffles([FromBody] WaffleRequest request, CancellationToken cancellationToken)
    {
        var response = await waffleService.MakeWafflesAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }
}