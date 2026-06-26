package io.lemonlion.breakfast.persistence.cosmos;

import com.azure.cosmos.ConsistencyLevel;
import com.azure.cosmos.CosmosClient;
import com.azure.cosmos.CosmosClientBuilder;
import com.azure.cosmos.CosmosContainer;
import com.azure.cosmos.CosmosDatabase;
import com.fasterxml.jackson.databind.ObjectMapper;
import io.lemonlion.breakfast.config.CosmosConfig;
import io.lemonlion.breakfast.events.outbox.CosmosOutboxStore;
import io.lemonlion.breakfast.events.outbox.CosmosOutboxWriter;
import io.lemonlion.breakfast.events.outbox.OutboxStore;
import io.lemonlion.breakfast.events.outbox.OutboxWriter;
import io.lemonlion.breakfast.persistence.CosmosRepository;
import io.lemonlion.breakfast.storage.AuditLogDocument;
import io.lemonlion.breakfast.storage.OrderDocument;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/**
 * Wires Azure Cosmos beans. The {@code orders} container holds order + outbox documents (so the outbox
 * transactional batch is atomic); audit logs live in their own container. Connection settings come from
 * {@link CosmosConfig}; tests point these at a Testcontainers Cosmos emulator.
 */
@Configuration
public class CosmosConfiguration {

    private static final String ORDERS_CONTAINER = "orders";
    private static final String AUDIT_CONTAINER = "auditLogs";
    private static final String PARTITION_KEY_PATH = "/partitionKey";

    @Bean(destroyMethod = "close")
    public CosmosClient cosmosClient(CosmosConfig config) {
        // buildClient() eagerly reads the DatabaseAccount to initialise; against the emulator that read can
        // momentarily time out (HTTP 408) when several heavyweight Testcontainers backends compete for the
        // host at the tail of a multi-module reactor build. Retry the build itself so a transient init
        // timeout doesn't fail the whole Spring context.
        return withRetry(() -> new CosmosClientBuilder()
                .endpoint(config.getEndpoint())
                .key(config.getKey())
                .gatewayMode()
                .endpointDiscoveryEnabled(config.isEndpointDiscoveryEnabled())
                .consistencyLevel(ConsistencyLevel.SESSION)
                .buildClient());
    }

    @Bean
    public CosmosDatabase breakfastDatabase(CosmosClient client, CosmosConfig config) {
        withRetry(() -> client.createDatabaseIfNotExists(config.getDatabaseName()));
        return client.getDatabase(config.getDatabaseName());
    }

    /**
     * Retries a Cosmos control-plane call. The emulator can momentarily time out (HTTP 408) at startup
     * when several heavyweight Testcontainers backends compete for resources; a few retries ride that out.
     */
    private static void withRetry(Runnable action) {
        withRetry(() -> {
            action.run();
            return null;
        });
    }

    private static <T> T withRetry(java.util.function.Supplier<T> action) {
        RuntimeException last = null;
        for (int attempt = 1; attempt <= 5; attempt++) {
            try {
                return action.get();
            } catch (RuntimeException e) {
                last = e;
                try {
                    Thread.sleep(2000L);
                } catch (InterruptedException ie) {
                    Thread.currentThread().interrupt();
                    throw e;
                }
            }
        }
        throw last;
    }

    @Bean
    public CosmosRepository<OrderDocument> orderRepository(CosmosDatabase database) {
        return new CosmosRepositoryImpl<>(ordersContainer(database), OrderDocument.class, "order", "createdAt");
    }

    @Bean
    public CosmosRepository<AuditLogDocument> auditLogRepository(CosmosDatabase database) {
        return new CosmosRepositoryImpl<>(auditContainer(database), AuditLogDocument.class, null, null);
    }

    @Bean
    public OutboxWriter outboxWriter(CosmosDatabase database, ObjectMapper objectMapper) {
        return new CosmosOutboxWriter(ordersContainer(database), objectMapper);
    }

    @Bean
    public OutboxStore outboxStore(CosmosDatabase database) {
        return new CosmosOutboxStore(ordersContainer(database));
    }

    @Bean
    public io.lemonlion.breakfast.storage.IdempotencyStore idempotencyStore(
            CosmosDatabase database, ObjectMapper objectMapper) {
        return new io.lemonlion.breakfast.storage.CosmosIdempotencyStore(ordersContainer(database), objectMapper);
    }

    private static CosmosContainer ordersContainer(CosmosDatabase database) {
        withRetry(() -> database.createContainerIfNotExists(ORDERS_CONTAINER, PARTITION_KEY_PATH));
        return database.getContainer(ORDERS_CONTAINER);
    }

    private static CosmosContainer auditContainer(CosmosDatabase database) {
        withRetry(() -> database.createContainerIfNotExists(AUDIT_CONTAINER, PARTITION_KEY_PATH));
        return database.getContainer(AUDIT_CONTAINER);
    }
}
