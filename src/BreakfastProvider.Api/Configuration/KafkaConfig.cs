using Confluent.Kafka;

namespace BreakfastProvider.Api.Configuration;

public class KafkaConfig
{
    public string DomainName { get; init; } = "";
    public string SourceUrl { get; init; } = "";
    public string BootstrapServers { get; init; } = "";
    
    /// <summary>
    /// Contains configurations for producer topics.
    /// </summary>
    public Dictionary<string, TopicConfiguration> ProducerConfigurations { get; init; } = new();
    
    /// <summary>
    /// Contains configurations for consumer topics.
    /// </summary>
    public Dictionary<string, TopicConfiguration> ConsumerConfigurations { get; init; } = new();
    public TimeSpan FlushTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public Acks Acknowledgements { get; init; } = Acks.Leader;
    public int MessageTimeoutInMilliseconds { get; init; }
    public string? SslCaLocation { get; set; }
}

public record TopicConfiguration
{
    public string TopicName { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string ApiSecret { get; set; } = "";
}
