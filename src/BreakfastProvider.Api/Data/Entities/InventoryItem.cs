namespace BreakfastProvider.Api.Data.Entities;

public class InventoryItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal ReorderLevel { get; set; }
    public DateTime LastRestockedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
