using System.ComponentModel;
using BreakfastProvider.Api.Events;

namespace BreakfastProvider.Api.Models.Events;

[Description("Published when an inventory item's stock level changes.")]
public class InventoryStockUpdatedEvent : IPubSubEvent
{
    [Description("Inventory item ID.")]
    public int ItemId { get; set; }

    [Description("Name of the inventory item.")]
    public string Name { get; set; } = string.Empty;

    [Description("Previous quantity in stock.")]
    public decimal PreviousQuantity { get; set; }

    [Description("New quantity in stock.")]
    public decimal NewQuantity { get; set; }

    [Description("Timestamp of the update (ISO 8601 format).")]
    public DateTime UpdatedAt { get; set; }
}
