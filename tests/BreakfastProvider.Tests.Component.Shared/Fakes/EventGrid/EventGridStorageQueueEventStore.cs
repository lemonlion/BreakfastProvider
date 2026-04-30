namespace BreakfastProvider.Tests.Component.Shared.Fakes.EventGrid;

/// <summary>
/// An <see cref="IPublishedEventStore"/> backed by the
/// <see cref="EventGridQueueDrainer"/>.  Events published by the API flow
/// through the Docker EventGrid simulator, which delivers them to an Azurite
/// storage queue — proving end-to-end subscription delivery via realistic
/// infrastructure.
///
/// Each call to <see cref="GetPublishedEventsAsync{T}"/> drains any new messages
/// from the queue, then returns all collected events matching the configured
/// source event type name.
/// </summary>
public class EventGridStorageQueueEventStore(
    EventGridQueueDrainer drainer,
    string sourceEventTypeName) : IPublishedEventStore
{
    public async Task<IReadOnlyList<T>> GetPublishedEventsAsync<T>() where T : class
    {
        await drainer.DrainAsync();
        return drainer.GetEventsBySourceName<T>(sourceEventTypeName);
    }
}
