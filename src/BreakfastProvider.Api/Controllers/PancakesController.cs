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
public class PancakesController(
    IRecipeLogger recipeLogger,
    PubSubEventPublisher<PancakeBatchCompletedEvent> batchCompletedPublisher,
    EventHubEventPublisher<EquipmentAlertEvent> equipmentAlertPublisher) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PancakeResponse>> MakePancakes([FromBody] PancakeRequest request, CancellationToken cancellationToken)
    {
        var batchId = Guid.NewGuid();
        var ingredients = new List<string> { request.Milk!, request.Flour!, request.Eggs! };

        var response = new PancakeResponse
        {
            BatchId = batchId,
            Ingredients = ingredients,
            Toppings = request.Toppings,
            CreatedAt = DateTime.UtcNow
        };

        await recipeLogger.LogRecipeAsync(new RecipeLogEvent
        {
            OrderId = batchId,
            RecipeType = "Pancakes",
            Ingredients = ingredients,
            Toppings = request.Toppings,
            LoggedAt = DateTime.UtcNow
        }, cancellationToken);

        await batchCompletedPublisher.PublishEvent(new PancakeBatchCompletedEvent
        {
            BatchId = batchId,
            Ingredients = ingredients,
            Toppings = request.Toppings,
            CompletedAt = DateTime.UtcNow
        }, cancellationToken);

        await equipmentAlertPublisher.PublishEvent(new EquipmentAlertEvent
        {
            AlertId = Guid.NewGuid(),
            BatchId = batchId,
            EquipmentName = "Griddle",
            AlertType = "UsageCycleCompleted",
            AlertedAt = DateTime.UtcNow
        }, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }
}