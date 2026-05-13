namespace BreakfastProvider.Tests.Component.Shared.Models.RecipeCosts;

public class TestRecipeCostRequest
{
    public string? RecipeName { get; set; }
    public List<string>? Ingredients { get; set; }
    public decimal TotalCost { get; set; }
    public string? Currency { get; set; }
}
