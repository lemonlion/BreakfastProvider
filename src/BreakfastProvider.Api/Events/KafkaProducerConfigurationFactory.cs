using Confluent.Kafka;
using Microsoft.Extensions.Options;
using BreakfastProvider.Api.Configuration;

namespace BreakfastProvider.Api.Events;

public class KafkaProducerConfigurationFactory : IKafkaProducerConfigurationFactory
{
    private readonly ProgramSettings _programSettings;
    private readonly KafkaConfig _kafkaSettings;
    private readonly ProgramConfig _programConfig;
    
    public KafkaProducerConfigurationFactory(
        IOptions<ProgramSettings> programSettings,
        IOptions<KafkaConfig> kafkaSettings,
        ProgramConfig programConfig)
    {
        _programSettings = programSettings.Value;
        _kafkaSettings = kafkaSettings.Value;
        _programConfig = programConfig;
    }

    public ProducerConfig GetProducerConfig<TValue>()
    {
        var topicConfiguration = GetTopicConfiguration<TValue>();
        if (topicConfiguration is null)
        {
            throw new ArgumentException($"No topic configuration found for type {typeof(TValue).Name}");
        }
        return CreateProducerConfig(topicConfiguration);
    }
    
    private ProducerConfig CreateProducerConfig(TopicConfiguration topicConfiguration)
        => new()
        {
            ClientId = $"{_programConfig.Namespace}.{_programConfig.Name}@{_programSettings.InstanceId}",
            BootstrapServers = _kafkaSettings.BootstrapServers,
            SaslMechanism = SaslMechanism.Plain,
            SaslUsername = topicConfiguration.ApiKey,
            SaslPassword = topicConfiguration.ApiSecret,
            SecurityProtocol = SecurityProtocol.SaslSsl,
            Acks = _kafkaSettings.Acknowledgements,
            SslEndpointIdentificationAlgorithm = SslEndpointIdentificationAlgorithm.None,
            SslCaLocation = _kafkaSettings.SslCaLocation
        };
    
    private TopicConfiguration GetTopicConfiguration<TValue>()
        => _kafkaSettings.ProducerConfigurations[typeof(TValue).Name];
}
