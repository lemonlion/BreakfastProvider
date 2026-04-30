namespace BreakfastProvider.Api.Events.Outbox;

public interface IOutboxWriter
{
    /// <summary>
    /// Atomically writes a business document and its associated outbox message
    /// in a single Cosmos DB transactional batch. Both items must share the same
    /// <paramref name="partitionKey"/>.
    /// </summary>
    Task WriteAsync<TDocument, TEvent>(TDocument document, TEvent @event, string partitionKey, string destination, CancellationToken cancellationToken = default)
        where TDocument : class
        where TEvent : class;
}
