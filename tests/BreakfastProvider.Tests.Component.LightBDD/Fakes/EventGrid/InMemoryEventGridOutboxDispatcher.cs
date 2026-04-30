using BreakfastProvider.Api.Events.Outbox;
using BreakfastProvider.Api.Storage;

namespace BreakfastProvider.Tests.Component.LightBDD.Fakes.EventGrid;

/// <summary>
/// In-memory replacement for <see cref="EventGridOutboxDispatcher"/> used in test mode.
/// Writes dispatched outbox messages directly to the <see cref="InMemoryEventGridPublisherStore"/>
/// so test assertions can verify event publishing via the outbox path.
/// </summary>
public class InMemoryEventGridOutboxDispatcher(InMemoryEventGridPublisherStore store) : IOutboxDispatcher
{
    public string Destination => OutboxDestinations.EventGrid;

    public Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        store.AddRawJson(message.Payload);
        return Task.CompletedTask;
    }
}
