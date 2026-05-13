namespace BreakfastProvider.Api.Models.Responses;

public class IngredientUsageResponse
{
    public string UsageId { get; set; } = string.Empty;
    public string IngredientName { get; set; } = string.Empty;
    public decimal QuantityUsed { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string RecipeName { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
}

public class IngredientUsageSummaryResponse
{
    public string IngredientName { get; set; } = string.Empty;
    public decimal TotalQuantityUsed { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int RecordCount { get; set; }
}
