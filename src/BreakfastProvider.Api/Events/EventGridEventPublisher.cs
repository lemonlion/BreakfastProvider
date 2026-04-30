using Azure.Messaging.EventGrid;
using BreakfastProvider.Api.Configuration;
using Microsoft.Extensions.Options;

namespace BreakfastProvider.Api.Events;

public class EventGridEventPublisher<T>(
    EventGridPublisherClient client,
    IOptions<EventGridConfig> config) : IEventPublisher<T> where T : class
{
    public async Task PublishAsync(T @event, CancellationToken cancellationToken = default)
    {
        if (!config.Value.IsEnabled) return;

        var eventGridEvent = new EventGridEvent(
            subject: config.Value.Subject,
            eventType: typeof(T).Name,
            dataVersion: "1.0",
            data: BinaryData.FromObjectAsJson(@event));

        await client.SendEventAsync(eventGridEvent, cancellationToken);
    }
}
