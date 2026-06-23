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

    /** Returns all items of this repository's type (used by query/filter domains like audit logs). */
    java.util.List<T> findAll();

    T upsert(T item, String partitionKey);
}
