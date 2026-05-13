using System.ComponentModel;
using BreakfastProvider.Api.Events;

namespace BreakfastProvider.Api.Models.Events;

[Description("Consumed when a recipe cost calculation is completed.")]
public class RecipeCostCalculatedEvent : IKafkaEvent
{
    [Description("Unique cost calculation identifier.")]
    public Guid CalculationId { get; set; }

    [Description("Name of the recipe.")]
    public string RecipeName { get; set; } = string.Empty;

    [Description("List of ingredients used in the recipe.")]
    public List<string> Ingredients { get; set; } = [];

    [Description("Total cost of the recipe in currency units.")]
    public decimal TotalCost { get; set; }

    [Description("Currency code (e.g. GBP, USD).")]
    public string Currency { get; set; } = "GBP";

    [Description("Timestamp when the cost was calculated (ISO 8601 format).")]
    public DateTime CalculatedAt { get; set; }
}
