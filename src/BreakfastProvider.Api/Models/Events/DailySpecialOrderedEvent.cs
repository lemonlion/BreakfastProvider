using System.ComponentModel;
using BreakfastProvider.Api.Events;

namespace BreakfastProvider.Api.Models.Events;

[Description("Published when a daily special is ordered.")]
public class DailySpecialOrderedEvent : IPubSubEvent
{
    [Description("Order ID for the daily special.")]
    public Guid OrderId { get; set; }

    [Description("Name of the daily special.")]
    public string SpecialName { get; set; } = string.Empty;

    [Description("Customer name.")]
    public string CustomerName { get; set; } = string.Empty;

    [Description("Remaining order count for this special.")]
    public int RemainingOrders { get; set; }

    [Description("Timestamp when the order was placed (ISO 8601 format).")]
    public DateTime OrderedAt { get; set; }
}
