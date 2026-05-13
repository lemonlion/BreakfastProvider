using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Telemetry;
using Google.Cloud.BigQuery.V2;

namespace BreakfastProvider.Api.Services;

public interface IIngredientUsageService
{
    Task<IngredientUsageResponse> RecordAsync(IngredientUsageRequest request, CancellationToken cancellationToken = default);
    Task<List<IngredientUsageSummaryResponse>> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<List<IngredientUsageResponse>> ListByIngredientAsync(string ingredientName, CancellationToken cancellationToken = default);
}

public class IngredientUsageService(BigQueryClient bigQueryClient, ILogger<IngredientUsageService> logger) : IIngredientUsageService
{
    private const string DatasetId = "breakfast_analytics";
    private const string TableId = "ingredient_usage";

    public async Task<IngredientUsageResponse> RecordAsync(IngredientUsageRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("IngredientUsageService.Record");

        var usageId = Guid.NewGuid().ToString();
        var recordedAt = DateTime.UtcNow;

        var row = new BigQueryInsertRow
        {
            ["usage_id"] = usageId,
            ["ingredient_name"] = request.IngredientName,
            ["quantity_used"] = (double)request.QuantityUsed,
            ["unit"] = request.Unit,
            ["recipe_name"] = request.RecipeName,
            ["recorded_at"] = recordedAt.ToString("yyyy-MM-dd HH:mm:ss")
        };

        await bigQueryClient.InsertRowsAsync(DatasetId, TableId, [row], cancellationToken: cancellationToken);

        logger.LogInformation("Ingredient usage {UsageId} recorded for {IngredientName}", usageId, request.IngredientName);

        return new IngredientUsageResponse
        {
            UsageId = usageId,
            IngredientName = request.IngredientName!,
            QuantityUsed = request.QuantityUsed,
            Unit = request.Unit!,
            RecipeName = request.RecipeName!,
            RecordedAt = recordedAt
        };
    }

    public async Task<List<IngredientUsageSummaryResponse>> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("IngredientUsageService.GetSummary");

        var sql = $@"SELECT ingredient_name, SUM(quantity_used) as total_quantity, unit, COUNT(*) as record_count
                     FROM `{DatasetId}.{TableId}`
                     GROUP BY ingredient_name, unit
                     ORDER BY total_quantity DESC";

        var results = await bigQueryClient.ExecuteQueryAsync(sql, parameters: null, cancellationToken: cancellationToken);

        return results.Select(row => new IngredientUsageSummaryResponse
        {
            IngredientName = (string)row["ingredient_name"],
            TotalQuantityUsed = Convert.ToDecimal((double)row["total_quantity"]),
            Unit = (string)row["unit"],
            RecordCount = (int)(long)row["record_count"]
        }).ToList();
    }

    public async Task<List<IngredientUsageResponse>> ListByIngredientAsync(string ingredientName, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("IngredientUsageService.ListByIngredient");

        var sql = $@"SELECT usage_id, ingredient_name, quantity_used, unit, recipe_name, recorded_at
                     FROM `{DatasetId}.{TableId}`
                     WHERE ingredient_name = @ingredientName
                     ORDER BY recorded_at DESC";

        var parameters = new[] { new BigQueryParameter("ingredientName", BigQueryDbType.String, ingredientName) };
        var results = await bigQueryClient.ExecuteQueryAsync(sql, parameters, cancellationToken: cancellationToken);

        return results.Select(row => new IngredientUsageResponse
        {
            UsageId = (string)row["usage_id"],
            IngredientName = (string)row["ingredient_name"],
            QuantityUsed = Convert.ToDecimal((double)row["quantity_used"]),
            Unit = (string)row["unit"],
            RecipeName = (string)row["recipe_name"],
            RecordedAt = DateTime.Parse((string)row["recorded_at"])
        }).ToList();
    }
}
