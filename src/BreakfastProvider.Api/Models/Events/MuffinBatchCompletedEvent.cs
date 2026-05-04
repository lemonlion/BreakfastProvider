using System.ComponentModel;
using BreakfastProvider.Api.Events;

namespace BreakfastProvider.Api.Models.Events;

[Description("Published when an apple cinnamon muffin batch is completed.")]
public class MuffinBatchCompletedEvent : IPubSubEvent
{
    [Description("Batch ID.")]
    public Guid BatchId { get; set; }

    [Description("List of ingredients used.")]
    public List<string> Ingredients { get; set; } = [];

    [Description("List of toppings applied.")]
    public List<string> Toppings { get; set; } = [];

    [Description("Baking temperature in degrees Celsius.")]
    public int BakingTemperature { get; set; }

    [Description("Timestamp when the batch was completed (ISO 8601 format).")]
    public DateTime CompletedAt { get; set; }
}
