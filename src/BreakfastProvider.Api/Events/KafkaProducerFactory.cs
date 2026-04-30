using Confluent.Kafka;

namespace BreakfastProvider.Api.Events;

public class KafkaProducerFactory : IProducerFactory
{
    private readonly IKafkaProducerConfigurationFactory _kafkaProducerConfigurationFactory;
    private readonly ILogger<KafkaProducerFactory> _logger;
    
    public KafkaProducerFactory(
        IKafkaProducerConfigurationFactory kafkaProducerConfigurationFactory,
        ILogger<KafkaProducerFactory> logger)
    {
        _kafkaProducerConfigurationFactory = kafkaProducerConfigurationFactory;
        _logger = logger;
    }
    
    public IProducer<Guid, TValue> Create<TValue>(ISerializer<TValue>? serializer = null)
    {
        serializer ??= new KafkaJsonSerializer<TValue>();
        var producerConfig = _kafkaProducerConfigurationFactory.GetProducerConfig<TValue>();
        var producerBuilder = new ProducerBuilder<Guid, TValue>(producerConfig)
            .SetKeySerializer(new KafkaGuidSerializer())
            .SetValueSerializer(serializer);

        producerBuilder.SetDiagnosticsHandlers(_logger);
        return producerBuilder.Build();
    }
}
