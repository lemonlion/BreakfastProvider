using Confluent.Kafka;

namespace BreakfastProvider.Api.Events;

public interface IKafkaProducerConfigurationFactory
{
    ProducerConfig GetProducerConfig<TValue>();
}
