using BreakfastProvider.Api.Events;
using BreakfastProvider.Api.Models.Events;
using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Telemetry;

namespace BreakfastProvider.Api.Services;

public class WaffleService(
    IRecipeLogger recipeLogger,
    PubSubEventPublisher<WaffleBatchCompletedEvent> batchCompletedPublisher,
    ILogger<WaffleService> logger) : IWaffleService
{
    public async Task<WaffleResponse> MakeWafflesAsync(WaffleRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("WaffleService.MakeWaffles");

        var batchId = Guid.NewGuid();
        var ingredients = new List<string> { request.Milk!, request.Flour!, request.Eggs!, request.Butter! };

        activity?.SetTag("waffle.batch_id", batchId.ToString());
        activity?.SetTag("waffle.ingredient_count", ingredients.Count);

        var response = PrepareBatch(batchId, ingredients, request.Toppings);

        await LogRecipeAsync(batchId, ingredients, request.Toppings, cancellationToken);
        await PublishBatchCompletedAsync(batchId, ingredients, request.Toppings, cancellationToken);

        return response;
    }

    private WaffleResponse PrepareBatch(Guid batchId, List<string> ingredients, List<string>? toppings)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("WaffleService.PrepareBatch");
        activity?.SetTag("waffle.batch_id", batchId.ToString());
        activity?.SetTag("waffle.topping_count", toppings?.Count ?? 0);

        logger.LogInformation("Preparing waffle batch {BatchId} with {IngredientCount} ingredients", batchId, ingredients.Count);

        return new WaffleResponse
        {
            BatchId = batchId,
            Ingredients = ingredients,
            Toppings = toppings,
            CreatedAt = DateTime.UtcNow
        };
    }

    private async Task LogRecipeAsync(Guid batchId, List<string> ingredients, List<string>? toppings, CancellationToken cancellationToken)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("WaffleService.LogRecipe");
        activity?.SetTag("waffle.batch_id", batchId.ToString());

        await recipeLogger.LogRecipeAsync(new RecipeLogEvent
        {
            OrderId = batchId,
            RecipeType = "Waffles",
            Ingredients = ingredients,
            Toppings = toppings,
            LoggedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    private async Task PublishBatchCompletedAsync(Guid batchId, List<string> ingredients, List<string>? toppings, CancellationToken cancellationToken)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("WaffleService.PublishBatchCompleted");
        activity?.SetTag("waffle.batch_id", batchId.ToString());

        await batchCompletedPublisher.PublishEvent(new WaffleBatchCompletedEvent
        {
            BatchId = batchId,
            Ingredients = ingredients,
            Toppings = toppings,
            CompletedAt = DateTime.UtcNow
        }, cancellationToken);
    }
}
