package io.lemonlion.breakfast.persistence.cosmos;

import com.azure.cosmos.CosmosException;
import java.util.function.Supplier;

/**
 * Bounded retry for transient Cosmos data-plane failures. The emulator's gateway can momentarily refuse
 * connections (surfacing as HTTP 503) or time out (408) when many writes/queries compete for it under
 * load; production Cosmos likewise expects callers to retry throttling/service-unavailable responses.
 * A few short retries ride that out without masking real errors (4xx such as 404/400/409 are rethrown
 * immediately).
 */
public final class CosmosRetry {

    private static final int MAX_ATTEMPTS = 5;
    private static final long BACKOFF_MS = 250L;

    private CosmosRetry() {
    }

    public static <T> T onTransient(Supplier<T> operation) {
        RuntimeException last = null;
        for (int attempt = 1; attempt <= MAX_ATTEMPTS; attempt++) {
            try {
                return operation.get();
            } catch (CosmosException e) {
                if (!isTransient(e)) {
                    throw e;
                }
                last = e;
            } catch (RuntimeException e) {
                if (!isConnectionRefused(e)) {
                    throw e;
                }
                last = e;
            }
            sleep(BACKOFF_MS * attempt);
        }
        throw last;
    }

    private static boolean isTransient(CosmosException e) {
        int code = e.getStatusCode();
        return code == 503 || code == 408 || code == 449 || isConnectionRefused(e);
    }

    private static boolean isConnectionRefused(Throwable e) {
        for (Throwable t = e; t != null; t = t.getCause()) {
            String message = t.getMessage();
            if (message != null && message.contains("Connection refused")) {
                return true;
            }
        }
        return false;
    }

    private static void sleep(long millis) {
        try {
            Thread.sleep(millis);
        } catch (InterruptedException ie) {
            Thread.currentThread().interrupt();
            throw new IllegalStateException("Interrupted during Cosmos retry backoff", ie);
        }
    }
}
