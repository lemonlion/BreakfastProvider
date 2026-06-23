package io.lemonlion.breakfast.persistence;

import java.util.Optional;

/**
 * Twin of C# {@code ICosmosRepository<T>}. The seam that lets the SUT swap a real Azure Cosmos-backed
 * implementation (Testcontainers "docker" mode) for an in-memory, Kronikol4J-tracked fake ("memory" mode).
 */
public interface CosmosRepository<T> {

    T create(T item, String partitionKey);

    Optional<T> findById(String id, String partitionKey);

    /** Returns up to {@code limit} items after {@code offset}, newest first, plus the total count. */
    PagedItems<T> queryPaged(int offset, int limit);

    T upsert(T item, String partitionKey);
}
