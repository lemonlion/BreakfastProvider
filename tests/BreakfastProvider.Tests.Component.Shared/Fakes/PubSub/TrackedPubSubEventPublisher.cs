using BreakfastProvider.Api.Events;
using TestTrackingDiagrams.Tracking;

namespace BreakfastProvider.Tests.Component.Shared.Fakes.PubSub;

/// <summary>
/// Decorator around <see cref="PubSubEventPublisher{T}"/> that logs publish
/// operations to <see cref="MessageTracker"/> so that Pub/Sub events appear
/// in the PlantUML sequence diagrams. Extends the base class so it can replace
/// the in-memory publisher in DI.
/// </summary>
public class TrackedPubSubEventPublisher<T>(
    PubSubEventPublisher<T> inner,
    MessageTracker tracker) : PubSubEventPublisher<T> where T : IPubSubEvent
{
    private const string Protocol = "Publish (Pub/Sub)";
    private const string ServiceName = "Google Cloud Pub/Sub";

    public override async Task PublishEvent(T @event, CancellationToken cancellationToken = default)
    {
        await inner.PublishEvent(@event, cancellationToken);
        tracker.TrackSendEvent(
            protocol: Protocol,
            destinationName: ServiceName,
            destinationUri: new Uri($"pubsub:///{typeof(T).Name}"),
            payload: @event);
    }
}
