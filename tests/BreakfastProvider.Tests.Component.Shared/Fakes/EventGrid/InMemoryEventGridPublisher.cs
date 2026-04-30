using System.Collections.Concurrent;
using System.Text.Json;
using BreakfastProvider.Api.Events;

namespace BreakfastProvider.Tests.Component.Shared.Fakes.EventGrid;

/// <summary>
/// Shared store that all <see cref="InMemoryEventGridPublisher{T}"/> instances write to.
/// Stores events as JSON strings so callers can deserialise to any structurally-compatible
/// type without knowing the original source type.
/// </summary>
public class InMemoryEventGridPublisherStore : IPublishedEventStore
{
    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ConcurrentBag<string> _publishedEvents = [];

    public void Add<T>(T @event) where T : class
        => _publishedEvents.Add(JsonSerializer.Serialize(@event));

    public void AddRawJson(string json)
        => _publishedEvents.Add(json);

    public Task<IReadOnlyList<T>> GetPublishedEventsAsync<T>() where T : class
    {
        IReadOnlyList<T> events = _publishedEvents
            .Select(json => JsonSerializer.Deserialize<T>(json, DeserializeOptions)!)
            .ToList();
        return Task.FromResult(events);
    }
}

public class InMemoryEventGridPublisher<T>(InMemoryEventGridPublisherStore store) : IEventPublisher<T>
    where T : class
{
    public Task PublishAsync(T @event, CancellationToken cancellationToken = default)
    {
        store.Add(@event);
        return Task.CompletedTask;
    }
}
