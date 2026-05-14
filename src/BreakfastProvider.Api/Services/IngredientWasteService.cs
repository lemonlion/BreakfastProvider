using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Telemetry;
using Google.Cloud.BigQuery.V2;

namespace BreakfastProvider.Api.Services;

public interface IIngredientWasteService
{
    Task<IngredientWasteResponse> RecordAsync(IngredientWasteRequest request, CancellationToken cancellationToken = default);
    Task<List<IngredientWasteResponse>> ListByRecipeAsync(string recipeName, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string wasteId, CancellationToken cancellationToken = default);
}

public class IngredientWasteService(BigQueryClient bigQueryClient, ILogger<IngredientWasteService> logger) : IIngredientWasteService
{
    private const string DatasetId = "breakfast_analytics";
    private const string TableId = "ingredient_waste";

    public async Task<IngredientWasteResponse> RecordAsync(IngredientWasteRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("IngredientWasteService.Record");

        var wasteId = Guid.NewGuid().ToString();
        var recordedAt = DateTime.UtcNow;

        var row = new BigQueryInsertRow
        {
            ["waste_id"] = wasteId,
            ["ingredient_name"] = request.IngredientName,
            ["quantity_wasted"] = (double)request.QuantityWasted,
            ["unit"] = request.Unit,
            ["recipe_name"] = request.RecipeName,
            ["reason"] = request.Reason,
            ["recorded_at"] = recordedAt.ToString("yyyy-MM-dd HH:mm:ss")
        };

        await bigQueryClient.InsertRowsAsync(DatasetId, TableId, [row], cancellationToken: cancellationToken);

        logger.LogInformation("Ingredient waste {WasteId} recorded for {IngredientName} in recipe {RecipeName}",
            wasteId, request.IngredientName, request.RecipeName);

        return new IngredientWasteResponse
        {
            WasteId = wasteId,
            IngredientName = request.IngredientName!,
            QuantityWasted = request.QuantityWasted,
            Unit = request.Unit!,
            RecipeName = request.RecipeName!,
            Reason = request.Reason!,
            RecordedAt = recordedAt
        };
    }

    public async Task<List<IngredientWasteResponse>> ListByRecipeAsync(string recipeName, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("IngredientWasteService.ListByRecipe");

        var sql = $@"SELECT waste_id, ingredient_name, quantity_wasted, unit, recipe_name, reason, recorded_at
                     FROM `{DatasetId}.{TableId}`
                     WHERE recipe_name = @recipeName
                     ORDER BY recorded_at DESC";

        var parameters = new[] { new BigQueryParameter("recipeName", BigQueryDbType.String, recipeName) };
        var results = await bigQueryClient.ExecuteQueryAsync(sql, parameters, cancellationToken: cancellationToken);

        return results.Select(row => new IngredientWasteResponse
        {
            WasteId = (string)row["waste_id"],
            IngredientName = (string)row["ingredient_name"],
            QuantityWasted = Convert.ToDecimal((double)row["quantity_wasted"]),
            Unit = (string)row["unit"],
            RecipeName = (string)row["recipe_name"],
            Reason = (string)row["reason"],
            RecordedAt = DateTime.Parse((string)row["recorded_at"])
        }).ToList();
    }

    public async Task<bool> DeleteAsync(string wasteId, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("IngredientWasteService.Delete");

        var sql = $@"DELETE FROM `{DatasetId}.{TableId}` WHERE waste_id = @wasteId";
        var parameters = new[] { new BigQueryParameter("wasteId", BigQueryDbType.String, wasteId) };

        var result = await bigQueryClient.ExecuteQueryAsync(sql, parameters, cancellationToken: cancellationToken);

        logger.LogInformation("Ingredient waste {WasteId} deleted", wasteId);
        return true;
    }
}
