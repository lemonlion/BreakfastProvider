namespace BreakfastProvider.Api.Models.Responses;

public class IngredientWasteResponse
{
    public string WasteId { get; set; } = string.Empty;
    public string IngredientName { get; set; } = string.Empty;
    public decimal QuantityWasted { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string RecipeName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
}
