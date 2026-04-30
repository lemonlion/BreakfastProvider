using BreakfastProvider.Api.Events.Outbox;
using TestTrackingDiagrams.Tracking;

namespace BreakfastProvider.Tests.Component.Shared.Fakes.Tracking;

/// <summary>
/// Decorator around <see cref="IOutboxWriter"/> that logs EventGrid-bound
/// outbox writes to <see cref="MessageTracker"/> so that they appear in
/// the PlantUML sequence diagrams.
///
/// The app publishes events exclusively through the outbox pattern:
/// OrderService → IOutboxWriter → Cosmos → OutboxProcessor → IOutboxDispatcher.
/// Because OutboxProcessor is a BackgroundService (no HttpContext), tracking
/// must happen here — at the IOutboxWriter level — while the original HTTP
/// request is still in scope and test identity headers are available.
///
/// Uses <see cref="MessageTracker"/> from the core TestTrackingDiagrams package
/// with <see cref="MessageTrackerOptions.UseHttpContextCorrelation"/> = true,
/// which reads test identity from the HTTP request headers propagated by
/// <c>TestTrackingMessageHandler</c>.
/// </summary>
public class TrackedOutboxWriter(
    IOutboxWriter inner,
    MessageTracker tracker) : IOutboxWriter
{
    private const string Protocol = "Publish (Event Grid)";
    private const string ServiceName = "Event Grid";

    public async Task WriteAsync<TDocument, TEvent>(TDocument document, TEvent @event, string partitionKey, string destination, CancellationToken cancellationToken = default)
        where TDocument : class
        where TEvent : class
    {
        await inner.WriteAsync(document, @event, partitionKey, destination, cancellationToken);

        if (!string.Equals(destination, OutboxDestinations.EventGrid, StringComparison.OrdinalIgnoreCase))
            return;

        tracker.TrackSendEvent(
            protocol: Protocol,
            destinationName: ServiceName,
            destinationUri: new Uri($"eventgrid://topics/{typeof(TEvent).Name}"),
            payload: @event);
    }
}
