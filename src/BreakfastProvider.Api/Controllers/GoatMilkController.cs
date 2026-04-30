using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Api.HttpClients;
using BreakfastProvider.Api.Models.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BreakfastProvider.Api.Controllers;

[ApiController]
[Route("goat-milk")]
[Produces("application/json")]
public class GoatMilkController(IHttpClientFactory httpClientFactory, IOptions<FeatureSwitchesConfig> featureSwitches, ILogger<GoatMilkController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<GoatMilkResponse>> GetGoatMilk(CancellationToken cancellationToken)
    {
        if (!featureSwitches.Value.IsGoatMilkEnabled)
            return NotFound(new ProblemDetails { Title = "Feature Disabled", Detail = "Goat milk is not currently available.", Status = 404 });

        var client = httpClientFactory.CreateClient(HttpClientNames.GoatService);

        try
        {
            using var response = await client.GetAsync("goat-milk", cancellationToken);
            if (!response.IsSuccessStatusCode)
                return StatusCode(StatusCodes.Status502BadGateway,
                    new ProblemDetails { Title = "Goat Service Unavailable", Detail = "The Goat Service returned an error.", Status = 502 });

            var goatMilkResponse = await response.Content.ReadFromJsonAsync<GoatMilkResponse>(cancellationToken);
            if (goatMilkResponse is null)
                return StatusCode(StatusCodes.Status502BadGateway,
                    new ProblemDetails { Title = "Goat Service Unavailable", Detail = "The Goat Service returned an invalid response.", Status = 502 });

            return goatMilkResponse;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Goat Service is unreachable");
            return StatusCode(StatusCodes.Status502BadGateway,
                new ProblemDetails { Title = "Goat Service Unavailable", Detail = "The Goat Service is unreachable.", Status = 502 });
        }
    }
}
