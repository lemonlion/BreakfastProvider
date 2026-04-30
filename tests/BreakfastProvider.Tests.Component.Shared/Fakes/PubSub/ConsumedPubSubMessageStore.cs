using System.Collections.Concurrent;
using System.Text.Json;

namespace BreakfastProvider.Tests.Component.Shared.Fakes.PubSub;

public class ConsumedPubSubMessageStore
{
    private readonly ConcurrentBag<StoredMessage> _consumedEvents = [];

    /// <summary>
    /// Fired synchronously after a message is stored.  Since
    /// <see cref="InMemoryPubSubEventPublisher{T}"/> calls <see cref="Add{T}"/>
    /// inside the HTTP request pipeline, subscribers execute within the same
    /// ASP.NET Core request context — which allows <c>MessageTracker</c> to
    /// attribute tracking events to the correct test diagram.
    /// </summary>
    public event Action<string, string>? MessageStored;

    public void Add<T>(T @event)
    {
        var json = JsonSerializer.Serialize(@event);
        var eventType = typeof(T).Name;
        _consumedEvents.Add(new StoredMessage(eventType, json));

        try { MessageStored?.Invoke(eventType, json); }
        catch { /* subscriber errors must not break the publisher */ }
    }

    public void AddRawJson(string eventTypeName, string json)
        => _consumedEvents.Add(new StoredMessage(eventTypeName, json));

    private static readonly JsonSerializerOptions CaseInsensitiveOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public IReadOnlyList<T> GetMessages<T>(string sourceEventTypeName) where T : class
    {
        return _consumedEvents
            .Where(e => e.EventType.Equals(sourceEventTypeName, StringComparison.OrdinalIgnoreCase))
            .Select(e => JsonSerializer.Deserialize<T>(e.Json, CaseInsensitiveOptions)!)
            .ToList();
    }

    private record StoredMessage(string EventType, string Json);
}
