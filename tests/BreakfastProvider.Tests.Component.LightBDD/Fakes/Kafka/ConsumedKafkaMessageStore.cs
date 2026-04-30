using System.Collections.Concurrent;
using System.Text.Json;
using Confluent.Kafka;

namespace BreakfastProvider.Tests.Component.LightBDD.Fakes.Kafka;

public class ConsumedKafkaMessageStore
{
    private readonly ConcurrentBag<StoredMessage> _consumedEvents = [];

    public void Add<TKey, TValue>(Message<TKey, TValue> message)
    {
        var json = JsonSerializer.Serialize(message.Value);
        var key = message.Key?.ToString() ?? string.Empty;
        _consumedEvents.Add(new StoredMessage(typeof(TValue).Name, key, json));
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
