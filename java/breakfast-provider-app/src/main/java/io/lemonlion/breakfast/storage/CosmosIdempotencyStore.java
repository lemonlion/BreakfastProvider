package io.lemonlion.breakfast.storage;

import com.azure.cosmos.CosmosContainer;
import com.azure.cosmos.CosmosException;
import com.azure.cosmos.models.CosmosItemRequestOptions;
import com.azure.cosmos.models.PartitionKey;
import com.fasterxml.jackson.databind.ObjectMapper;
import java.util.Optional;

/** Cosmos-backed {@link IdempotencyStore} (records live in the shared {@code orders} container). */
public class CosmosIdempotencyStore implements IdempotencyStore {

    private static final int NOT_FOUND = 404;

    private final CosmosContainer container;
    private final ObjectMapper objectMapper;

    public CosmosIdempotencyStore(CosmosContainer container, ObjectMapper objectMapper) {
        this.container = container;
        this.objectMapper = objectMapper;
    }

    @Override
    public <T> Optional<T> tryGet(String key, Class<T> type) {
        try {
            IdempotencyRecord record = container.readItem(key, new PartitionKey(key), IdempotencyRecord.class).getItem();
            return Optional.of(objectMapper.readValue(record.getPayload(), type));
        } catch (CosmosException e) {
            if (e.getStatusCode() == NOT_FOUND) {
                return Optional.empty();
            }
            throw e;
        } catch (Exception e) {
            throw new IllegalStateException("Failed to read idempotency record " + key, e);
        }
    }

    @Override
    public void set(String key, int statusCode, Object response) {
        try {
            IdempotencyRecord record = new IdempotencyRecord();
            record.setId(key);
            record.setPartitionKey(key);
            record.setStatusCode(statusCode);
            record.setPayload(objectMapper.writeValueAsString(response));
            container.upsertItem(record, new PartitionKey(key), new CosmosItemRequestOptions());
        } catch (Exception e) {
            throw new IllegalStateException("Failed to store idempotency record " + key, e);
        }
    }
}
