namespace BreakfastProvider.Tests.Component.Shared.Models.IngredientUsage;

public class TestIngredientUsageRequest
{
    public string? IngredientName { get; set; }
    public decimal QuantityUsed { get; set; }
    public string? Unit { get; set; }
    public string? RecipeName { get; set; }
}
