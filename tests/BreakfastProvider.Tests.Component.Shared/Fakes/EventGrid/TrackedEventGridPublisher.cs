using BreakfastProvider.Api.Events;
using BreakfastProvider.Tests.Component.Shared.Fakes.Tracking;

namespace BreakfastProvider.Tests.Component.Shared.Fakes.EventGrid;

/// <summary>
/// Decorator around <see cref="IEventPublisher{T}"/> that logs
/// request/response pairs to the <see cref="EventPublishingTracker"/> so that
/// EventGrid event publications appear in the PlantUML sequence diagrams.
/// </summary>
public class TrackedEventGridPublisher<T>(
    IEventPublisher<T> inner,
    EventPublishingTracker tracker) : IEventPublisher<T>
    where T : class
{
    private const string Protocol = "Publish (Event Grid)";
    private const string ServiceName = "Event Grid";

    private static readonly Uri DestinationUri = new("https://eventgrid-server/" + typeof(T).Name);

    public async Task PublishAsync(T @event, CancellationToken cancellationToken = default)
    {
        var requestResponseId = tracker.CreateRequestLog(Protocol, ServiceName, DestinationUri, @event);

        await inner.PublishAsync(@event, cancellationToken);

        tracker.CreateResponseLog(Protocol, ServiceName, DestinationUri, requestResponseId,
            new { Status = "Published", EventType = typeof(T).Name });
    }
}
