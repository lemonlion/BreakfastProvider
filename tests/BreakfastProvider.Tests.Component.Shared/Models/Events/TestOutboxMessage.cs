namespace BreakfastProvider.Tests.Component.Shared.Models.Events;

public class TestOutboxMessage
{
    public string Id { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
}
