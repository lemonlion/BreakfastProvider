using BreakfastProvider.Api.Events;
using BreakfastProvider.Api.Models.Events;
using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BreakfastProvider.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
public class WafflesController(
    IRecipeLogger recipeLogger,
    PubSubEventPublisher<WaffleBatchCompletedEvent> batchCompletedPublisher) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WaffleResponse>> MakeWaffles([FromBody] WaffleRequest request, CancellationToken cancellationToken)
    {
        var batchId = Guid.NewGuid();
        var ingredients = new List<string> { request.Milk!, request.Flour!, request.Eggs!, request.Butter! };

        var response = new WaffleResponse
        {
            BatchId = batchId,
            Ingredients = ingredients,
            Toppings = request.Toppings,
            CreatedAt = DateTime.UtcNow
        };

        await recipeLogger.LogRecipeAsync(new RecipeLogEvent
        {
            OrderId = batchId,
            RecipeType = "Waffles",
            Ingredients = ingredients,
            Toppings = request.Toppings,
            LoggedAt = DateTime.UtcNow
        }, cancellationToken);

        await batchCompletedPublisher.PublishEvent(new WaffleBatchCompletedEvent
        {
            BatchId = batchId,
            Ingredients = ingredients,
            Toppings = request.Toppings,
            CompletedAt = DateTime.UtcNow
        }, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }
}