namespace BreakfastProvider.Api.Configuration;

public class PubSubConfig
{
    public string ProjectId { get; init; } = "";
    public string SourceUrl { get; init; } = "";
    public string DomainName { get; init; } = "";

    /// <summary>
    /// Contains configurations for publisher topics.
    /// </summary>
    public Dictionary<string, PubSubTopicConfiguration> PublisherConfigurations { get; init; } = new();

    /// <summary>
    /// Contains configurations for subscriber subscriptions.
    /// </summary>
    public Dictionary<string, PubSubSubscriptionConfiguration> SubscriberConfigurations { get; init; } = new();

    public int PublishTimeoutInMilliseconds { get; init; } = 30000;
}

public record PubSubTopicConfiguration
{
    public string TopicId { get; set; } = "";
}

public record PubSubSubscriptionConfiguration
{
    public string SubscriptionId { get; set; } = "";
}
