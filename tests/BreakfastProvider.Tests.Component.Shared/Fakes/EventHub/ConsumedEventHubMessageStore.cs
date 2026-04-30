using System.Collections.Concurrent;
using System.Text.Json;

namespace BreakfastProvider.Tests.Component.Shared.Fakes.EventHub;

/// <summary>
/// Thread-safe in-memory store for Event Hub messages produced during tests.
/// Fires <see cref="MessageStored"/> synchronously within the HTTP request
/// context so that <see cref="MessageTracker"/> can attribute the consumption
/// to the correct test's PlantUML diagram.
/// </summary>
public class ConsumedEventHubMessageStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ConcurrentBag<StoredMessage> _messages = [];

    public event Action<string, string>? MessageStored;

    public void Add<T>(T @event) where T : class
    {
        var eventTypeName = typeof(T).Name.Replace("Event", "") + "Event";
        var json = JsonSerializer.Serialize(@event);
        _messages.Add(new StoredMessage(eventTypeName, json));
        MessageStored?.Invoke(eventTypeName, json);
    }

    public IReadOnlyList<T> GetMessages<T>(string sourceEventTypeName) where T : class
    {
        return _messages
            .Where(m => m.EventTypeName == sourceEventTypeName)
            .Select(m => JsonSerializer.Deserialize<T>(m.Json, JsonOptions)!)
            .ToList();
    }

    private record StoredMessage(string EventTypeName, string Json);
}
