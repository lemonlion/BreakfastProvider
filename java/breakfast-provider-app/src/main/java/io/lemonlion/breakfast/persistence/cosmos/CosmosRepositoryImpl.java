package io.lemonlion.breakfast.persistence.cosmos;

import com.azure.cosmos.CosmosContainer;
import com.azure.cosmos.CosmosException;
import com.azure.cosmos.models.CosmosItemRequestOptions;
import com.azure.cosmos.models.CosmosQueryRequestOptions;
import com.azure.cosmos.models.PartitionKey;
import com.azure.cosmos.util.CosmosPagedIterable;
import io.lemonlion.breakfast.persistence.CosmosRepository;
import io.lemonlion.breakfast.persistence.PagedItems;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

/**
 * Azure Cosmos-backed {@link CosmosRepository}. Querying applies offset/limit in memory (twin of the C#
 * repository's note that it materializes then pages); an optional {@code docType} discriminator scopes a
 * shared container, and an optional order-by field returns newest-first.
 */
public class CosmosRepositoryImpl<T> implements CosmosRepository<T> {

    private static final int NOT_FOUND = 404;

    private final CosmosContainer container;
    private final Class<T> type;
    private final String docType;
    private final String orderByField;

    public CosmosRepositoryImpl(CosmosContainer container, Class<T> type, String docType, String orderByField) {
        this.container = container;
        this.type = type;
        this.docType = docType;
        this.orderByField = orderByField;
    }

    @Override
    public T create(T item, String partitionKey) {
        return container.createItem(item, new PartitionKey(partitionKey), new CosmosItemRequestOptions()).getItem();
    }

    @Override
    public Optional<T> findById(String id, String partitionKey) {
        try {
            return Optional.ofNullable(container.readItem(id, new PartitionKey(partitionKey), type).getItem());
        } catch (CosmosException e) {
            if (e.getStatusCode() == NOT_FOUND) {
                return Optional.empty();
            }
            throw e;
        }
    }

    @Override
    public PagedItems<T> queryPaged(int offset, int limit) {
        String query = "SELECT * FROM c"
                + (docType != null ? " WHERE c.docType = '" + docType + "'" : "")
                + (orderByField != null ? " ORDER BY c." + orderByField + " DESC" : "");
        CosmosPagedIterable<T> results = container.queryItems(query, new CosmosQueryRequestOptions(), type);
        List<T> all = new ArrayList<>();
        results.forEach(all::add);
        int from = Math.min(Math.max(offset, 0), all.size());
        int to = Math.min(from + Math.max(limit, 0), all.size());
        return new PagedItems<>(new ArrayList<>(all.subList(from, to)), all.size());
    }

    @Override
    public List<T> findAll() {
        String query = "SELECT * FROM c" + (docType != null ? " WHERE c.docType = '" + docType + "'" : "");
        CosmosPagedIterable<T> results = container.queryItems(query, new CosmosQueryRequestOptions(), type);
        List<T> all = new ArrayList<>();
        results.forEach(all::add);
        return all;
    }

    @Override
    public T upsert(T item, String partitionKey) {
        return container.upsertItem(item, new PartitionKey(partitionKey), new CosmosItemRequestOptions()).getItem();
    }
}
