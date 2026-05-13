using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BreakfastProvider.Api.Controllers;

[ApiController]
[Route("recipe-reviews")]
[Produces("application/json")]
[Consumes("application/json")]
public class RecipeReviewsController(IRecipeReviewService recipeReviewService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RecipeReviewResponse>> Create([FromBody] RecipeReviewRequest request, CancellationToken cancellationToken)
    {
        var response = await recipeReviewService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("{reviewId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecipeReviewResponse>> GetById(string reviewId, CancellationToken cancellationToken)
    {
        var response = await recipeReviewService.GetByIdAsync(reviewId, cancellationToken);
        if (response is null) return NotFound();
        return response;
    }

    [HttpGet("recipe/{recipeName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RecipeReviewResponse>>> ListByRecipe(string recipeName, CancellationToken cancellationToken)
    {
        var results = await recipeReviewService.ListByRecipeAsync(recipeName, cancellationToken);
        return results;
    }
}
