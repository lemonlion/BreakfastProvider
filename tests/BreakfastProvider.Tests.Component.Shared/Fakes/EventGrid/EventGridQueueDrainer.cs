using System.Collections.Concurrent;
using System.Text.Json;
using Azure.Storage.Queues;

namespace BreakfastProvider.Tests.Component.Shared.Fakes.EventGrid;

/// <summary>
/// Drains EventGrid events that the Docker EventGrid simulator delivered to an
/// Azurite storage queue.  This proves that events flow through the realistic
/// infrastructure (API → EventGrid simulator → storage queue subscriber)
/// before being read back by tests.
///
/// Thread-safe: a single global instance is shared across all parallel tests.
/// Tests filter by unique identifiers (e.g. order ID) set per-test.
///
/// Messages in the queue are Base64-encoded JSON arrays of EventGrid-schema events.
/// Each array element has <c>eventType</c> and <c>data</c> fields.
/// </summary>
public sealed class EventGridQueueDrainer
{
    private const string QueueName = "eventgrid-events";
    private const int MaxMessagesPerBatch = 32;

    private readonly ConcurrentBag<DrainedEvent> _events = [];
    private readonly QueueClient _queueClient;

    public EventGridQueueDrainer(string connectionString)
    {
        _queueClient = new QueueClient(
            connectionString,
            QueueName,
            new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
    }

    /// <summary>
    /// Reads all available messages from the Azurite storage queue and adds
    /// their EventGrid event payloads to the in-memory collection.  Messages
    /// are deleted after successful processing so they won't be read again.
    /// Safe to call from multiple tests concurrently.
    /// </summary>
    public async Task DrainAsync()
    {
        var exists = await _queueClient.ExistsAsync();
        if (!exists.Value)
            return;

        while (true)
        {
            var response = await _queueClient.ReceiveMessagesAsync(MaxMessagesPerBatch);
            var messages = response.Value;

            if (messages.Length == 0)
                break;

            foreach (var message in messages)
            {
                try
                {
                    var body = message.Body.ToString();

                    using var doc = JsonDocument.Parse(body);
                    var root = doc.RootElement;

                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var element in root.EnumerateArray())
                            StoreEvent(element);
                    }
                    else
                    {
                        StoreEvent(root);
                    }

                    await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EventGridQueueDrainer] Failed to parse queue message {message.MessageId}: {ex.Message}");
                }
            }
        }
    }

    private void StoreEvent(JsonElement eventElement)
    {
        var eventType = eventElement.TryGetProperty("eventType", out var et)
            ? et.GetString() ?? "unknown"
            : "unknown";

        var data = eventElement.TryGetProperty("data", out var d)
            ? d.Clone()
            : default;

        _events.Add(new DrainedEvent(eventType, data));
    }

    public IReadOnlyList<T> GetEvents<T>() where T : class
    {
        var typeName = typeof(T).Name;
        return GetEventsBySourceName<T>(typeName);
    }

    private static readonly JsonSerializerOptions CaseInsensitiveOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public IReadOnlyList<T> GetEventsBySourceName<T>(string sourceEventTypeName) where T : class
    {
        return _events
            .Where(e => e.EventType.Equals(sourceEventTypeName, StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Data.Deserialize<T>(CaseInsensitiveOptions))
            .Where(e => e is not null)
            .Cast<T>()
            .ToList();
    }

    public void Clear() => _events.Clear();

    private record DrainedEvent(string EventType, JsonElement Data);
}
