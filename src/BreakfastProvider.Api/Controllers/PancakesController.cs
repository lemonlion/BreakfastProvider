using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BreakfastProvider.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
public class PancakesController(IPancakeService pancakeService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PancakeResponse>> MakePancakes([FromBody] PancakeRequest request, CancellationToken cancellationToken)
    {
        var response = await pancakeService.MakePancakesAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }
}