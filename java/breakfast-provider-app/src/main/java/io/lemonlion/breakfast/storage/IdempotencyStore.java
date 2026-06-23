package io.lemonlion.breakfast.storage;

import java.util.Optional;

/** Twin of C# {@code IIdempotencyStore}: caches a prior response keyed by an idempotency key. */
public interface IdempotencyStore {

    <T> Optional<T> tryGet(String key, Class<T> type);

    void set(String key, int statusCode, Object response);
}
