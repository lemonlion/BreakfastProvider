using BreakfastProvider.Api.HttpClients;
using BreakfastProvider.Api.Services;
using BreakfastProvider.Api.Telemetry;
using Google.Cloud.BigQuery.V2;

namespace BreakfastProvider.Api.Reporting;

public interface IRecipeCostAnalysisService
{
    Task ProcessCostCalculationAsync(Guid calculationId, string recipeName, List<string> ingredients, decimal totalCost, string currency, DateTime calculatedAt, CancellationToken cancellationToken = default);
}

public class RecipeCostAnalysisService(
    BigQueryClient bigQueryClient,
    INotificationClient notificationClient,
    IHttpClientFactory httpClientFactory,
    ILogger<RecipeCostAnalysisService> logger) : IRecipeCostAnalysisService
{
    private const string DatasetId = "breakfast_analytics";
    private const string TableId = "recipe_costs";

    public async Task ProcessCostCalculationAsync(Guid calculationId, string recipeName, List<string> ingredients, decimal totalCost, string currency, DateTime calculatedAt, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("RecipeCostAnalysisService.ProcessCostCalculation");

        // Step 1: Write to BigQuery
        var row = new BigQueryInsertRow
        {
            ["calculation_id"] = calculationId.ToString(),
            ["recipe_name"] = recipeName,
            ["ingredients"] = string.Join(",", ingredients),
            ["total_cost"] = (double)totalCost,
            ["currency"] = currency,
            ["calculated_at"] = calculatedAt.ToString("yyyy-MM-dd HH:mm:ss")
        };

        await bigQueryClient.InsertRowsAsync(DatasetId, TableId, [row], cancellationToken: cancellationToken);
        logger.LogInformation("Recipe cost {CalculationId} stored in BigQuery for recipe {RecipeName}", calculationId, recipeName);

        // Step 2: Send notification via gRPC
        var (success, notificationId) = await notificationClient.SendOrderConfirmationAsync(
            calculationId.ToString(), recipeName, ingredients.Count, cancellationToken);

        logger.LogInformation("Notification sent for cost calculation {CalculationId}: Success={Success}, NotificationId={NotificationId}",
            calculationId, success, notificationId);

        // Step 3: Call Kitchen Service to update recipe costs
        var kitchenClient = httpClientFactory.CreateClient(HttpClientNames.KitchenService);
        await kitchenClient.PostAsJsonAsync("prepare", new
        {
            RecipeName = recipeName,
            TotalCost = totalCost,
            Currency = currency,
            Ingredients = ingredients
        }, cancellationToken);

        logger.LogInformation("Kitchen service notified about cost calculation {CalculationId} for recipe {RecipeName}", calculationId, recipeName);
    }
}
