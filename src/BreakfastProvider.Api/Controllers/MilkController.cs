using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BreakfastProvider.Api.Controllers;

[ApiController]
[Route("milk")]
[Produces("application/json")]
public class MilkController(IMilkSourcingService milkSourcingService, ILogger<MilkController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<MilkResponse>> GetMilk(CancellationToken cancellationToken)
    {
        try
        {
            var milkResponse = await milkSourcingService.SourceFromCowAsync(cancellationToken);
            return milkResponse;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Cow Service is unreachable");
            return StatusCode(StatusCodes.Status502BadGateway,
                new ProblemDetails { Title = "Cow Service Unavailable", Detail = ex.Message, Status = 502 });
        }
    }
}
