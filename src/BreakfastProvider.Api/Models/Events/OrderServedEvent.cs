using System.ComponentModel;
using BreakfastProvider.Api.Events;

namespace BreakfastProvider.Api.Models.Events;

[Description("Consumed when an order is served to a customer.")]
public class OrderServedEvent : IKafkaEvent
{
    [Description("Unique service identifier.")]
    public Guid ServiceId { get; set; }

    [Description("Identifier of the order that was served.")]
    public Guid OrderId { get; set; }

    [Description("Type of item served (e.g. Pancakes, Waffles).")]
    public string ItemType { get; set; } = string.Empty;

    [Description("Seconds the customer waited between ordering and being served.")]
    public decimal WaitSeconds { get; set; }

    [Description("Timestamp when the order was served (ISO 8601 format).")]
    public DateTime ServedAt { get; set; }
}
