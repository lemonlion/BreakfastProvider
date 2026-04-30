namespace BreakfastProvider.Tests.Component.Shared.Models.Feedback;

public class TestFeedbackRequest
{
    public string? CustomerName { get; set; }
    public string? OrderId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}
