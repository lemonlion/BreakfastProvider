using System.ComponentModel;
using BreakfastProvider.Api.Events;

namespace BreakfastProvider.Api.Models.Events;

[Description("Published when a new breakfast order is created.")]
public class OrderCreatedEvent : IEventGridEvent
{
    [Description("Unique order identifier.")]
    public Guid OrderId { get; set; }

    [Description("Name of the customer who placed the order.")]
    public string CustomerName { get; set; } = string.Empty;

    [Description("Number of items in the order.")]
    public int ItemCount { get; set; }

    [Description("Table number for the order, if applicable.")]
    public int? TableNumber { get; set; }

    [Description("Timestamp when the order was created (ISO 8601 format).")]
    public DateTime CreatedAt { get; set; }
}
