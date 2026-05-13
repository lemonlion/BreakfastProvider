using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BreakfastProvider.Api.Controllers;

[ApiController]
[Route("ingredient-usage")]
[Produces("application/json")]
[Consumes("application/json")]
public class IngredientUsageController(IIngredientUsageService ingredientUsageService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IngredientUsageResponse>> Record([FromBody] IngredientUsageRequest request, CancellationToken cancellationToken)
    {
        var response = await ingredientUsageService.RecordAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<IngredientUsageSummaryResponse>>> GetSummary(CancellationToken cancellationToken)
    {
        var results = await ingredientUsageService.GetSummaryAsync(cancellationToken);
        return results;
    }

    [HttpGet("ingredient/{ingredientName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<IngredientUsageResponse>>> ListByIngredient(string ingredientName, CancellationToken cancellationToken)
    {
        var results = await ingredientUsageService.ListByIngredientAsync(ingredientName, cancellationToken);
        return results;
    }
}
