using System.ComponentModel;
using BreakfastProvider.Api.Events;

namespace BreakfastProvider.Api.Models.Events;

[Description("Content for the Recipe Log Event.")]
public class RecipeLogEvent : IKafkaEvent
{
    [Description("Order ID.")]
    public Guid OrderId { get; set; }

    [Description("Recipe type (e.g. Pancakes, Waffles).")]
    public string RecipeType { get; set; } = string.Empty;

    [Description("List of ingredients used.")]
    public List<string> Ingredients { get; set; } = [];

    [Description("List of toppings applied.")]
    public List<string> Toppings { get; set; } = [];

    [Description("Timestamp when the recipe was logged (ISO 8601 format).")]
    public DateTime LoggedAt { get; set; }
}
