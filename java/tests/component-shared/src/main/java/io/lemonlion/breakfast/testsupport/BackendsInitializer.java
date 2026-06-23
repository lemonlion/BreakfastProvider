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
                "downstream.kitchen-service-url=" + BreakfastBackends.kitchenUrl(),
                "downstream.supplier-service-url=" + BreakfastBackends.supplierUrl(),
                // EventGrid has no emulator; the outbox still records + processes the message.
                "event-grid.enabled=false",
                // Fast outbox polling so processing assertions don't wait long.
                "outbox.polling-interval-seconds=1"
        ).applyTo(context.getEnvironment());
    }
}
