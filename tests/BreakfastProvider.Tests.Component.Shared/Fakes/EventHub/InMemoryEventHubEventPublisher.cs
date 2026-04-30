using BreakfastProvider.Api.Events;

namespace BreakfastProvider.Tests.Component.Shared.Fakes.EventHub;

/// <summary>
/// In-memory replacement for <see cref="EventHubEventPublisher{T}"/> that
/// stores published events in a <see cref="ConsumedEventHubMessageStore"/>
/// instead of sending them to Azure Event Hubs.
/// </summary>
public class InMemoryEventHubEventPublisher<T>(ConsumedEventHubMessageStore store)
    : EventHubEventPublisher<T> where T : class, IEventHubEvent
{
    public override Task PublishEvent(T @event, CancellationToken cancellationToken = default)
    {
        store.Add(@event);
        return Task.CompletedTask;
    }
}
