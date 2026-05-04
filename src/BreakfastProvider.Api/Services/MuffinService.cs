using BreakfastProvider.Api.Events;
using BreakfastProvider.Api.Models.Events;
using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Telemetry;

namespace BreakfastProvider.Api.Services;

public class MuffinService(
    IRecipeLogger recipeLogger,
    PubSubEventPublisher<MuffinBatchCompletedEvent> batchCompletedPublisher,
    EventHubEventPublisher<EquipmentAlertEvent> equipmentAlertPublisher,
    ILogger<MuffinService> logger) : IMuffinService
{
    public async Task<MuffinResponse> MakeMuffinsAsync(MuffinRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("MuffinService.MakeMuffins");

        var batchId = Guid.NewGuid();
        var ingredients = new List<string> { request.Milk!, request.Flour!, request.Eggs!, request.Apples!, request.Cinnamon! };
        var toppings = request.Toppings.Select(t => t.Name!).ToList();

        activity?.SetTag("muffin.batch_id", batchId.ToString());
        activity?.SetTag("muffin.ingredient_count", ingredients.Count);

        var response = PrepareBatch(batchId, ingredients, toppings, request.Baking!);

        await LogRecipeAsync(batchId, ingredients, toppings, cancellationToken);
        await PublishBatchCompletedAsync(batchId, ingredients, toppings, request.Baking!.Temperature, cancellationToken);
        await PublishEquipmentAlertAsync(batchId, cancellationToken);

        return response;
    }

    private MuffinResponse PrepareBatch(Guid batchId, List<string> ingredients, List<string> toppings, BakingProfile baking)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("MuffinService.PrepareBatch");
        activity?.SetTag("muffin.batch_id", batchId.ToString());
        activity?.SetTag("muffin.topping_count", toppings.Count);

        logger.LogInformation("Preparing muffin batch {BatchId} with {IngredientCount} ingredients at {Temperature}°C for {Duration}min",
            batchId, ingredients.Count, baking.Temperature, baking.DurationMinutes);

        return new MuffinResponse
        {
            BatchId = batchId,
            Ingredients = ingredients,
            Toppings = toppings,
            BakingTemperature = baking.Temperature,
            BakingDuration = baking.DurationMinutes,
            CreatedAt = DateTime.UtcNow
        };
    }

    private async Task LogRecipeAsync(Guid batchId, List<string> ingredients, List<string> toppings, CancellationToken cancellationToken)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("MuffinService.LogRecipe");
        activity?.SetTag("muffin.batch_id", batchId.ToString());

        await recipeLogger.LogRecipeAsync(new RecipeLogEvent
        {
            OrderId = batchId,
            RecipeType = "AppleCinnamonMuffins",
            Ingredients = ingredients,
            Toppings = toppings,
            LoggedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    private async Task PublishBatchCompletedAsync(Guid batchId, List<string> ingredients, List<string> toppings, int temperature, CancellationToken cancellationToken)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("MuffinService.PublishBatchCompleted");
        activity?.SetTag("muffin.batch_id", batchId.ToString());

        await batchCompletedPublisher.PublishEvent(new MuffinBatchCompletedEvent
        {
            BatchId = batchId,
            Ingredients = ingredients,
            Toppings = toppings,
            BakingTemperature = temperature,
            CompletedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    private async Task PublishEquipmentAlertAsync(Guid batchId, CancellationToken cancellationToken)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("MuffinService.PublishEquipmentAlert");
        activity?.SetTag("muffin.batch_id", batchId.ToString());

        await equipmentAlertPublisher.PublishEvent(new EquipmentAlertEvent
        {
            AlertId = Guid.NewGuid(),
            BatchId = batchId,
            EquipmentName = "Muffin Oven",
            AlertType = "UsageCycleCompleted",
            AlertedAt = DateTime.UtcNow
        }, cancellationToken);
    }
}
