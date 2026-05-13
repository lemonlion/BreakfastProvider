namespace BreakfastProvider.Tests.Component.Shared.Models.IngredientUsage;

public class TestIngredientUsageResponse
{
    public string UsageId { get; set; } = string.Empty;
    public string IngredientName { get; set; } = string.Empty;
    public decimal QuantityUsed { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string RecipeName { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
}

public class TestIngredientUsageSummaryResponse
{
    public string IngredientName { get; set; } = string.Empty;
    public decimal TotalQuantityUsed { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int RecordCount { get; set; }
}
