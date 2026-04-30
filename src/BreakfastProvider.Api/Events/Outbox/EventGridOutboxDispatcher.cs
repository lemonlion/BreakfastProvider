using Azure.Messaging.EventGrid;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Api.Storage;
using Microsoft.Extensions.Options;

namespace BreakfastProvider.Api.Events.Outbox;

public class EventGridOutboxDispatcher(
    EventGridPublisherClient client,
    IOptions<EventGridConfig> config) : IOutboxDispatcher
{
    public string Destination => OutboxDestinations.EventGrid;

    public async Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        if (!config.Value.IsEnabled) return;

        var eventGridEvent = new EventGridEvent(
            subject: config.Value.Subject,
            eventType: message.EventType,
            dataVersion: "1.0",
            data: BinaryData.FromString(message.Payload));

        await client.SendEventAsync(eventGridEvent, cancellationToken);
    }
}
