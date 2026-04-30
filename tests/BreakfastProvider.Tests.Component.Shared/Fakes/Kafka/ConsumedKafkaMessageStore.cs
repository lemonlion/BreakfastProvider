using System.Collections.Concurrent;
using System.Text.Json;
using Confluent.Kafka;

namespace BreakfastProvider.Tests.Component.Shared.Fakes.Kafka;

public class ConsumedKafkaMessageStore
{
    private readonly ConcurrentBag<StoredMessage> _consumedEvents = [];

    /// <summary>
    /// Fired synchronously after a message is stored.  Since
    /// <see cref="InMemoryProducer{TKey,TValue}"/> calls <see cref="Add{TKey,TValue}"/>
    /// inside the HTTP request pipeline, subscribers execute within the same
    /// ASP.NET Core request context — which allows <c>MessageTracker</c> to
    /// attribute tracking events to the correct test diagram.
    /// </summary>
    public event Action<string, string, string>? MessageStored;

    public void Add<TKey, TValue>(Message<TKey, TValue> message)
    {
        var json = JsonSerializer.Serialize(message.Value);
        var key = message.Key?.ToString() ?? string.Empty;
        var eventType = typeof(TValue).Name;
        _consumedEvents.Add(new StoredMessage(eventType, key, json));

        try { MessageStored?.Invoke(eventType, key, json); }
        catch { /* subscriber errors must not break the producer */ }
    }

    public void AddRawJson(string eventTypeName, string key, string json)
        => _consumedEvents.Add(new StoredMessage(eventTypeName, key, json));

    private static readonly JsonSerializerOptions CaseInsensitiveOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public IReadOnlyList<(string Key, T Message)> GetMessages<T>(string sourceEventTypeName) where T : class
    {
        return _consumedEvents
            .Where(e => e.EventType.Equals(sourceEventTypeName, StringComparison.OrdinalIgnoreCase))
            .Select(e => (e.Key, JsonSerializer.Deserialize<T>(e.Json, CaseInsensitiveOptions)!))
            .ToList();
    }

    private record StoredMessage(string EventType, string Key, string Json);
}
