using BreakfastProvider.Api.Events;
using BreakfastProvider.Api.Models.Events;
using BreakfastProvider.Api.Storage;
using BreakfastProvider.Api.Telemetry;

namespace BreakfastProvider.Api.Services;

public class RecipeLogger(
    ICosmosRepository<RecipeDocument> recipeRepository,
    KafkaEventPublisher<RecipeLogEvent> kafkaPublisher,
    ILogger<RecipeLogger> logger) : IRecipeLogger
{
    public async Task LogRecipeAsync(RecipeLogEvent recipe, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("RecipeLogger.LogRecipe");
        activity?.SetTag("recipe.type", recipe.RecipeType);
        activity?.SetTag("recipe.order_id", recipe.OrderId.ToString());

        var document = new RecipeDocument
        {
            PartitionKey = recipe.RecipeType,
            OrderId = recipe.OrderId,
            RecipeType = recipe.RecipeType,
            Ingredients = recipe.Ingredients,
            Toppings = recipe.Toppings,
            LoggedAt = recipe.LoggedAt
        };

        try
        {
            await recipeRepository.CreateAsync(document, document.PartitionKey, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist recipe log for order {OrderId}", recipe.OrderId);
        }

        try
        {
            await kafkaPublisher.PublishEvent(recipe, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish recipe event for order {OrderId}", recipe.OrderId);
        }

        DiagnosticsConfig.RecipesLogged.Add(1,
            new KeyValuePair<string, object?>("recipe.type", recipe.RecipeType));
    }
}
