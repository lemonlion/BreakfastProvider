using BreakfastProvider.Api.HttpClients;
using BreakfastProvider.Api.Models.Responses;
using Microsoft.AspNetCore.Mvc;

namespace BreakfastProvider.Api.Controllers;

[ApiController]
[Route("milk")]
[Produces("application/json")]
public class MilkController(IHttpClientFactory httpClientFactory, ILogger<MilkController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<MilkResponse>> GetMilk(CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(HttpClientNames.CowService);

        try
        {
            using var response = await client.GetAsync("milk", cancellationToken);
            if (!response.IsSuccessStatusCode)
                return StatusCode(StatusCodes.Status502BadGateway,
                    new ProblemDetails { Title = "Cow Service Unavailable", Detail = "The Cow Service returned an error.", Status = 502 });

            var milkResponse = await response.Content.ReadFromJsonAsync<MilkResponse>(cancellationToken);
            if (milkResponse is null)
                return StatusCode(StatusCodes.Status502BadGateway,
                    new ProblemDetails { Title = "Cow Service Unavailable", Detail = "The Cow Service returned an invalid response.", Status = 502 });

            return milkResponse;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Cow Service is unreachable");
            return StatusCode(StatusCodes.Status502BadGateway,
                new ProblemDetails { Title = "Cow Service Unavailable", Detail = "The Cow Service is unreachable.", Status = 502 });
        }
    }
}
