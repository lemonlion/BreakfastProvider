package io.lemonlion.breakfast.events.outbox;

/**
 * Twin of C# {@code IOutboxWriter}. Atomically persists a domain document and its outbox message
 * (Cosmos transactional batch in the real impl; a single in-memory commit in the fake).
 */
public interface OutboxWriter {

    /**
     * @param document    the domain document to store
     * @param event       the event to serialize into the outbox message payload
     * @param partitionKey the shared partition key (document and message must co-locate for the batch)
     * @param destination one of {@link OutboxDestinations}
     */
    <TDocument, TEvent> void write(TDocument document, TEvent event, String partitionKey, String destination);
}
