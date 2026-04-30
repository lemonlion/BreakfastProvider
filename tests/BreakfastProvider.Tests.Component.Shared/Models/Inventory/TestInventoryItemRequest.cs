namespace BreakfastProvider.Tests.Component.Shared.Models.Inventory;

public class TestInventoryItemRequest
{
    public string? Name { get; set; }
    public string? Category { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal ReorderLevel { get; set; }
}
