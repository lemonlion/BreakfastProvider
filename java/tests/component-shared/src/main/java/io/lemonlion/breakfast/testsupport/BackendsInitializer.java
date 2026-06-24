package io.lemonlion.breakfast.testsupport;

import org.springframework.boot.test.util.TestPropertyValues;
import org.springframework.context.ApplicationContextInitializer;
import org.springframework.context.ConfigurableApplicationContext;

/**
 * Starts the shared Testcontainers backends and points the SUT's configuration at them before the
 * Spring context refreshes. Framework-agnostic: usable from JUnit 5, TestNG, Spock and Cucumber bases
 * via {@code @ContextConfiguration(initializers = BackendsInitializer.class)}.
 */
public class BackendsInitializer implements ApplicationContextInitializer<ConfigurableApplicationContext> {

    @Override
    public void initialize(ConfigurableApplicationContext context) {
        BreakfastBackends.start();
        TestPropertyValues.of(
                "cosmos.endpoint=" + BreakfastBackends.cosmosEndpoint(),
                "cosmos.key=" + BreakfastBackends.cosmosKey(),
                "cosmos.database-name=breakfast",
                // Emulator advertises an unreachable internal IP; force the SDK to use only the gateway endpoint.
                "cosmos.endpoint-discovery-enabled=false",
                "spring.kafka.bootstrap-servers=" + BreakfastBackends.kafkaBootstrapServers(),
                // Relational store (SQL Server) for the EF Core-backed domains; auto-create the schema.
                "spring.datasource.url=" + BreakfastBackends.sqlServerJdbcUrl(),
                "spring.datasource.username=" + BreakfastBackends.sqlServerUsername(),
                "spring.datasource.password=" + BreakfastBackends.sqlServerPassword(),
                "spring.jpa.hibernate.ddl-auto=create-drop",
                "mongodb.uri=" + BreakfastBackends.mongoConnectionString(),
                "spanner.jdbc-url=" + BreakfastBackends.spannerJdbcUrl(),
                "bigquery.emulator-endpoint=" + BreakfastBackends.bigQueryEndpoint(),
                "bigquery.project-id=" + BreakfastBackends.bigQueryProjectId(),
                "pubsub.emulator-endpoint=" + BreakfastBackends.pubSubEndpoint(),
                "pubsub.project-id=" + BreakfastBackends.pubSubProjectId(),
                "pubsub.customer-feedback-subscription=" + BreakfastBackends.feedbackSubscription(),
                "pubsub.batch-completion-topic=batch-completions",
                "pubsub.batch-completion-subscription=batch-completions-sub",
                "downstream.kitchen-service-url=" + BreakfastBackends.kitchenUrl(),
                "downstream.supplier-service-url=" + BreakfastBackends.supplierUrl(),
                "downstream.cow-service-url=" + BreakfastBackends.cowUrl(),
                "downstream.goat-service-url=" + BreakfastBackends.goatUrl(),
                // EventGrid has no emulator; the outbox still records + processes the message.
                "event-grid.enabled=false",
                // Fast outbox polling so processing assertions don't wait long.
                "outbox.polling-interval-seconds=1",
                // gRPC: in-process server (no TCP port). The default in-process name lets the gRPC tests
                // reach it via GrpcSupport; an override context (e.g. rate-limiting) can set its own name
                // via @TestPropertySource so two live contexts don't collide on the same in-process name.
                "grpc.server.port=-1"
        ).applyTo(context.getEnvironment());

        if (context.getEnvironment().getProperty("grpc.server.in-process-name") == null) {
            TestPropertyValues.of("grpc.server.in-process-name=" + GrpcSupport.IN_PROCESS_NAME)
                    .applyTo(context.getEnvironment());
        }
    }
}
