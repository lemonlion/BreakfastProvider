using System.ComponentModel;
using BreakfastProvider.Api.Events;

namespace BreakfastProvider.Api.Models.Events;

[Description("Published when a new topping is added to the toppings catalogue.")]
public class ToppingCreatedEvent : IPubSubEvent
{
    [Description("Topping ID.")]
    public Guid ToppingId { get; set; }

    [Description("Name of the topping.")]
    public string Name { get; set; } = string.Empty;

    [Description("Category of the topping (e.g. fruit, sauce, nut).")]
    public string Category { get; set; } = string.Empty;

    [Description("Whether the topping is seasonal.")]
    public bool IsSeasonal { get; set; }

    [Description("Timestamp when the topping was created (ISO 8601 format).")]
    public DateTime CreatedAt { get; set; }
}
