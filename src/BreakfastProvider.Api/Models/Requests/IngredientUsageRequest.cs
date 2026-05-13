namespace BreakfastProvider.Api.Models.Requests;

public record IngredientUsageRequest
{
    public string? IngredientName { get; init; }
    public decimal QuantityUsed { get; init; }
    public string? Unit { get; init; }
    public string? RecipeName { get; init; }
}
