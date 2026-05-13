using System.ComponentModel;
using BreakfastProvider.Api.Events;

namespace BreakfastProvider.Api.Models.Events;

[Description("Consumed when customer feedback is received for a recipe.")]
public class CustomerFeedbackReceivedEvent : IPubSubEvent
{
    [Description("Unique feedback identifier.")]
    public Guid FeedbackId { get; set; }

    [Description("Name of the customer providing feedback.")]
    public string CustomerName { get; set; } = string.Empty;

    [Description("Name of the recipe being reviewed.")]
    public string RecipeName { get; set; } = string.Empty;

    [Description("Rating from 1 to 5.")]
    public int Rating { get; set; }

    [Description("Customer comments.")]
    public string Comments { get; set; } = string.Empty;

    [Description("Timestamp when the feedback was received (ISO 8601 format).")]
    public DateTime ReceivedAt { get; set; }
}
