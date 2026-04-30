namespace BreakfastProvider.Api.Models.Responses;

public record InventoryItemResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public string Unit { get; init; } = string.Empty;
    public decimal ReorderLevel { get; init; }
    public DateTime LastRestockedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}
