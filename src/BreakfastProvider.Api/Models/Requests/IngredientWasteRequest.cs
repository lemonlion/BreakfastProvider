namespace BreakfastProvider.Api.Models.Requests;

public record IngredientWasteRequest
{
    public string? IngredientName { get; init; }
    public decimal QuantityWasted { get; init; }
    public string? Unit { get; init; }
    public string? RecipeName { get; init; }
    public string? Reason { get; init; }
}
