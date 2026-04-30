using System.ComponentModel;
using BreakfastProvider.Api.Events;

namespace BreakfastProvider.Api.Models.Events;

[Description("Published when a new inventory item is added to the system.")]
public class InventoryItemAddedEvent : IPubSubEvent
{
    [Description("Inventory item ID.")]
    public int ItemId { get; set; }

    [Description("Name of the inventory item.")]
    public string Name { get; set; } = string.Empty;

    [Description("Category of the inventory item.")]
    public string Category { get; set; } = string.Empty;

    [Description("Initial quantity in stock.")]
    public decimal Quantity { get; set; }

    [Description("Unit of measurement (e.g. kg, litres, units).")]
    public string Unit { get; set; } = string.Empty;

    [Description("Timestamp when the item was added (ISO 8601 format).")]
    public DateTime AddedAt { get; set; }
}
