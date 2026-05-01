using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BreakfastProvider.Api.Controllers;

[ApiController]
[Route("goat-milk")]
[Produces("application/json")]
public class GoatMilkController(IMilkSourcingService milkSourcingService, IOptions<FeatureSwitchesConfig> featureSwitches, ILogger<GoatMilkController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<GoatMilkResponse>> GetGoatMilk(CancellationToken cancellationToken)
    {
        if (!featureSwitches.Value.IsGoatMilkEnabled)
            return NotFound(new ProblemDetails { Title = "Feature Disabled", Detail = "Goat milk is not currently available.", Status = 404 });

        try
        {
            var goatMilkResponse = await milkSourcingService.SourceFromGoatAsync(cancellationToken);
            return goatMilkResponse;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Goat Service is unreachable");
            return StatusCode(StatusCodes.Status502BadGateway,
                new ProblemDetails { Title = "Goat Service Unavailable", Detail = ex.Message, Status = 502 });
        }
    }
}
