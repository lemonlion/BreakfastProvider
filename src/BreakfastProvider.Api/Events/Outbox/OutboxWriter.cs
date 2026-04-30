using System.Text.Json;
using BreakfastProvider.Api.Storage;
using Microsoft.Azure.Cosmos;

namespace BreakfastProvider.Api.Events.Outbox;

public class OutboxWriter(Container container) : IOutboxWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task WriteAsync<TDocument, TEvent>(TDocument document, TEvent @event, string partitionKey, string destination, CancellationToken cancellationToken = default)
        where TDocument : class
        where TEvent : class
    {
        var eventType = typeof(TEvent).Name;
        var payload = JsonSerializer.Serialize(@event, SerializerOptions);

        var message = new OutboxMessage
        {
            PartitionKey = partitionKey,
            EventType = eventType,
            Destination = destination,
            Payload = payload,
            Status = OutboxMessageStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        var batch = container.CreateTransactionalBatch(new PartitionKey(partitionKey));
        batch.CreateItem(document);
        batch.CreateItem(message);

        using var response = await batch.ExecuteAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new CosmosException(
                $"Transactional batch failed with status {response.StatusCode}",
                response.StatusCode, 0, response.ActivityId, response.RequestCharge);
    }
}
