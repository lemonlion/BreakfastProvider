namespace BreakfastProvider.Api.Models.Requests;

public record InventoryItemRequest
{
    public string? Name { get; init; }
    public string? Category { get; init; }
    public decimal Quantity { get; init; }
    public string? Unit { get; init; }
    public decimal ReorderLevel { get; init; }
}
