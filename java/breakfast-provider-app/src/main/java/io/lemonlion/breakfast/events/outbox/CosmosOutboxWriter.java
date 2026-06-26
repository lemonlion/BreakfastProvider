package io.lemonlion.breakfast.events.outbox;

import com.azure.cosmos.CosmosContainer;
import com.azure.cosmos.models.CosmosBatch;
import com.azure.cosmos.models.CosmosBatchResponse;
import com.azure.cosmos.models.PartitionKey;
import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import io.lemonlion.breakfast.persistence.cosmos.CosmosRetry;
import io.lemonlion.breakfast.storage.OutboxMessage;
import io.lemonlion.breakfast.storage.OutboxMessageStatus;
import java.time.Instant;

/**
 * Twin of C# {@code OutboxWriter}: writes the domain document and its outbox message in a single Cosmos
 * transactional batch (same partition key), so an order is never committed without its event.
 */
public class CosmosOutboxWriter implements OutboxWriter {

    private final CosmosContainer container;
    private final ObjectMapper objectMapper;

    public CosmosOutboxWriter(CosmosContainer container, ObjectMapper objectMapper) {
        this.container = container;
        this.objectMapper = objectMapper;
    }

    @Override
    public <TDocument, TEvent> void write(TDocument document, TEvent event, String partitionKey, String destination) {
        String payload;
        try {
            payload = objectMapper.writeValueAsString(event);
        } catch (JsonProcessingException e) {
            throw new IllegalStateException("Failed to serialize outbox event " + event.getClass().getSimpleName(), e);
        }

        OutboxMessage message = new OutboxMessage();
        message.setPartitionKey(partitionKey);
        message.setEventType(event.getClass().getSimpleName());
        message.setDestination(destination);
        message.setPayload(payload);
        message.setStatus(OutboxMessageStatus.PENDING);
        message.setCreatedAt(Instant.now());

        CosmosRetry.onTransient(() -> {
            CosmosBatch batch = CosmosBatch.createCosmosBatch(new PartitionKey(partitionKey));
            batch.createItemOperation(document);
            batch.createItemOperation(message);
            CosmosBatchResponse response = container.executeCosmosBatch(batch);
            if (!response.isSuccessStatusCode()) {
                throw new IllegalStateException(
                        "Outbox transactional batch failed with status " + response.getStatusCode());
            }
            return null;
        });
    }
}
