namespace BreakfastProvider.Api.Models.Requests;

public record FeedbackRequest
{
    public string? CustomerName { get; init; }
    public string? OrderId { get; init; }
    public int Rating { get; init; }
    public string? Comment { get; init; }
}
