using Confluent.Kafka;
using BreakfastProvider.Api.Events;
using TestTrackingDiagrams.Extensions.Kafka;

namespace BreakfastProvider.Tests.Component.Shared.Fakes.Kafka;

public class TrackingKafkaProducerFactory(
    IProducerFactory innerFactory,
    KafkaTracker tracker,
    KafkaTrackingOptions options) : IProducerFactory
{
    public IProducer<Guid, TValue> Create<TValue>(ISerializer<TValue>? serializer = null)
    {
        var inner = innerFactory.Create(serializer);
        return new TrackingKafkaProducer<Guid, TValue>(inner, tracker, options);
    }
}
