namespace BreakfastProvider.Tests.Component.Shared.Models.Feedback;

public class TestFeedbackResponse
{
    public string FeedbackId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
