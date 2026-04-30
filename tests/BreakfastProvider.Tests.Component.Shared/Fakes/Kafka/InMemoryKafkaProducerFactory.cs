using Confluent.Kafka;
using BreakfastProvider.Api.Events;

namespace BreakfastProvider.Tests.Component.Shared.Fakes.Kafka;

public class InMemoryKafkaProducerFactory : IProducerFactory
{
    private readonly ConsumedKafkaMessageStore _consumedKafkaMessageStore;

    public InMemoryKafkaProducerFactory(ConsumedKafkaMessageStore consumedKafkaMessageStore)
    {
        _consumedKafkaMessageStore = consumedKafkaMessageStore;
    }
    
    public IProducer<Guid, TValue> Create<TValue>(ISerializer<TValue>? serializer = null)
    {
        return new InMemoryProducer<Guid, TValue>(_consumedKafkaMessageStore);
    }
}