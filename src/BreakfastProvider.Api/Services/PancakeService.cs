using BreakfastProvider.Api.Events;
using BreakfastProvider.Api.Models.Events;
using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Telemetry;

namespace BreakfastProvider.Api.Services;

public class PancakeService(
    IRecipeLogger recipeLogger,
    PubSubEventPublisher<PancakeBatchCompletedEvent> batchCompletedPublisher,
    EventHubEventPublisher<EquipmentAlertEvent> equipmentAlertPublisher,
    ILogger<PancakeService> logger) : IPancakeService
{
    public async Task<PancakeResponse> MakePancakesAsync(PancakeRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("PancakeService.MakePancakes");

        var batchId = Guid.NewGuid();
        var ingredients = new List<string> { request.Milk!, request.Flour!, request.Eggs! };

        activity?.SetTag("pancake.batch_id", batchId.ToString());
        activity?.SetTag("pancake.ingredient_count", ingredients.Count);

        var response = PrepareBatch(batchId, ingredients, request.Toppings);

        await LogRecipeAsync(batchId, ingredients, request.Toppings, cancellationToken);
        await PublishBatchCompletedAsync(batchId, ingredients, request.Toppings, cancellationToken);
        await PublishEquipmentAlertAsync(batchId, cancellationToken);

        return response;
    }

    private PancakeResponse PrepareBatch(Guid batchId, List<string> ingredients, List<string>? toppings)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("PancakeService.PrepareBatch");
        activity?.SetTag("pancake.batch_id", batchId.ToString());
        activity?.SetTag("pancake.topping_count", toppings?.Count ?? 0);

        logger.LogInformation("Preparing pancake batch {BatchId} with {IngredientCount} ingredients", batchId, ingredients.Count);

        return new PancakeResponse
        {
            BatchId = batchId,
            Ingredients = ingredients,
            Toppings = toppings,
            CreatedAt = DateTime.UtcNow
        };
    }

    private async Task LogRecipeAsync(Guid batchId, List<string> ingredients, List<string>? toppings, CancellationToken cancellationToken)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("PancakeService.LogRecipe");
        activity?.SetTag("pancake.batch_id", batchId.ToString());

        await recipeLogger.LogRecipeAsync(new RecipeLogEvent
        {
            OrderId = batchId,
            RecipeType = "Pancakes",
            Ingredients = ingredients,
            Toppings = toppings,
            LoggedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    private async Task PublishBatchCompletedAsync(Guid batchId, List<string> ingredients, List<string>? toppings, CancellationToken cancellationToken)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("PancakeService.PublishBatchCompleted");
        activity?.SetTag("pancake.batch_id", batchId.ToString());

        await batchCompletedPublisher.PublishEvent(new PancakeBatchCompletedEvent
        {
            BatchId = batchId,
            Ingredients = ingredients,
            Toppings = toppings,
            CompletedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    private async Task PublishEquipmentAlertAsync(Guid batchId, CancellationToken cancellationToken)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("PancakeService.PublishEquipmentAlert");
        activity?.SetTag("pancake.batch_id", batchId.ToString());

        await equipmentAlertPublisher.PublishEvent(new EquipmentAlertEvent
        {
            AlertId = Guid.NewGuid(),
            BatchId = batchId,
            EquipmentName = "Griddle",
            AlertType = "UsageCycleCompleted",
            AlertedAt = DateTime.UtcNow
        }, cancellationToken);
    }
}
