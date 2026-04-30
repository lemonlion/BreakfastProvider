using BreakfastProvider.Api.Events;

namespace BreakfastProvider.Tests.Component.Shared.Fakes.PubSub;

/// <summary>
/// In-memory replacement for <see cref="PubSubEventPublisher{T}"/> that writes to a
/// <see cref="ConsumedPubSubMessageStore"/> instead of Google Cloud Pub/Sub.
/// </summary>
public class InMemoryPubSubEventPublisher<T>(ConsumedPubSubMessageStore store) : PubSubEventPublisher<T> where T : IPubSubEvent
{
    public override Task PublishEvent(T @event, CancellationToken cancellationToken = default)
    {
        store.Add(@event);
        return Task.CompletedTask;
    }
}
