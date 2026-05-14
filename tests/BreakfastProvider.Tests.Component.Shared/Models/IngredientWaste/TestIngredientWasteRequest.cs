namespace BreakfastProvider.Tests.Component.Shared.Models.IngredientWaste;

public class TestIngredientWasteRequest
{
    public string? IngredientName { get; set; }
    public decimal QuantityWasted { get; set; }
    public string? Unit { get; set; }
    public string? RecipeName { get; set; }
    public string? Reason { get; set; }
}
