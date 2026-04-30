using Confluent.Kafka;

namespace BreakfastProvider.Api.Events;

public interface IProducerFactory
{
    public IProducer<Guid, TValue> Create<TValue>(ISerializer<TValue>? serializer = null);
}
