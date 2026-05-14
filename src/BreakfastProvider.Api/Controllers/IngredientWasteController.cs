using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BreakfastProvider.Api.Controllers;

[ApiController]
[Route("ingredient-waste")]
[Produces("application/json")]
[Consumes("application/json")]
public class IngredientWasteController(IIngredientWasteService ingredientWasteService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IngredientWasteResponse>> Record([FromBody] IngredientWasteRequest request, CancellationToken cancellationToken)
    {
        var response = await ingredientWasteService.RecordAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("recipe/{recipeName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<IngredientWasteResponse>>> ListByRecipe(string recipeName, CancellationToken cancellationToken)
    {
        var results = await ingredientWasteService.ListByRecipeAsync(recipeName, cancellationToken);
        return results;
    }

    [HttpDelete("{wasteId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string wasteId, CancellationToken cancellationToken)
    {
        await ingredientWasteService.DeleteAsync(wasteId, cancellationToken);
        return NoContent();
    }
}
