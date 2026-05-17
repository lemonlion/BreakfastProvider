using BreakfastProvider.Api.Events;
using Kronikol.Tracking;

namespace BreakfastProvider.Tests.Component.Shared.Fakes.EventHub;

/// <summary>
/// Decorator around <see cref="EventHubEventPublisher{T}"/> that logs publish
/// operations to <see cref="MessageTracker"/> so that Event Hub events appear
/// in the PlantUML sequence diagrams. Extends the base class so it can replace
/// the real publisher in DI.
/// </summary>
public class TrackedEventHubEventPublisher<T>(
    EventHubEventPublisher<T> inner,
    MessageTracker tracker) : EventHubEventPublisher<T> where T : class, IEventHubEvent
{
    private const string Protocol = "Publish (Event Hub)";
    private const string ServiceName = "Azure Event Hub";

    public override async Task PublishEvent(T @event, CancellationToken cancellationToken = default)
    {
        await inner.PublishEvent(@event, cancellationToken);
        tracker.TrackSendEvent(
            protocol: Protocol,
            destinationName: ServiceName,
            destinationUri: new Uri($"eventhub:///{typeof(T).Name}"),
            payload: @event);
    }
}
