namespace BreakfastProvider.Api.Configuration;

public class EventGridConfig
{
    public string Subject { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string TopicKey { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}