package io.lemonlion.breakfast.events.outbox;

import com.azure.cosmos.CosmosContainer;
import com.azure.cosmos.models.CosmosItemRequestOptions;
import com.azure.cosmos.models.CosmosQueryRequestOptions;
import com.azure.cosmos.models.PartitionKey;
import com.azure.cosmos.util.CosmosPagedIterable;
import io.lemonlion.breakfast.storage.OutboxMessage;
import java.util.ArrayList;
import java.util.List;

/** Cosmos-backed {@link OutboxStore} over the shared {@code orders} container ({@code docType = 'outbox'}). */
public class CosmosOutboxStore implements OutboxStore {

    private final CosmosContainer container;

    public CosmosOutboxStore(CosmosContainer container) {
        this.container = container;
    }

    @Override
    public List<OutboxMessage> findByStatus(String status, int limit) {
        String query = "SELECT * FROM c WHERE c.docType = 'outbox' AND c.status = '" + status + "'";
        return collect(query).stream().limit(Math.max(limit, 0)).toList();
    }

    @Override
    public List<OutboxMessage> findAll() {
        return collect("SELECT * FROM c WHERE c.docType = 'outbox'");
    }

    @Override
    public OutboxMessage update(OutboxMessage message) {
        return container.upsertItem(message, new PartitionKey(message.getPartitionKey()),
                new CosmosItemRequestOptions()).getItem();
    }

    @Override
    public OutboxMessage add(OutboxMessage message) {
        return container.createItem(message, new PartitionKey(message.getPartitionKey()),
                new CosmosItemRequestOptions()).getItem();
    }

    private List<OutboxMessage> collect(String query) {
        CosmosPagedIterable<OutboxMessage> results =
                container.queryItems(query, new CosmosQueryRequestOptions(), OutboxMessage.class);
        List<OutboxMessage> all = new ArrayList<>();
        results.forEach(all::add);
        return all;
    }
}
