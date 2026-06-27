package io.lemonlion.breakfast.testsupport;

import org.springframework.boot.test.util.TestPropertyValues;
import org.springframework.context.ApplicationContextInitializer;
import org.springframework.context.ConfigurableApplicationContext;

/**
 * Like {@link BackendsInitializer}, but points the SUT's relational store at a fresh, empty in-memory H2
 * database instead of the shared SQL Server container. Used only by the order-summaries "empty list when
 * no orders exist" scenario: that store is never empty in the shared docker suite (orders accumulate
 * across the whole JVM), so this context starts with an empty {@code order_summaries} and creates no
 * orders, letting the {@code orderSummaries} GraphQL query genuinely return {@code []}.
 *
 * <p>Everything else (Cosmos, Kafka, Mongo, Spanner, BigQuery, Pub/Sub, Event Hubs, the downstream fakes,
 * gRPC) is the real shared backend, so the SUT boots exactly as normal — only the relational datasource is
 * swapped. Order-summary rows are written in-process by {@code OrderServiceImpl} (not via a shared
 * topic/event), so no other context's orders can leak into this H2.
 */
public class EmptyReportingBackendsInitializer
        implements ApplicationContextInitializer<ConfigurableApplicationContext> {

    @Override
    public void initialize(ConfigurableApplicationContext context) {
        // Set a unique in-process gRPC name first so BackendsInitializer keeps it (it only defaults the
        // name when unset) and this extra live context doesn't collide with the shared one.
        TestPropertyValues.of("grpc.server.in-process-name=breakfast-grpc-empty-reporting")
                .applyTo(context.getEnvironment());

        new BackendsInitializer().initialize(context);

        // Override only the relational datasource with a fresh empty H2. TestPropertyValues merges into the
        // same "test" property source BackendsInitializer used, so these overwrite its SQL Server settings.
        TestPropertyValues.of(
                "spring.datasource.url=jdbc:h2:mem:empty-reporting;DB_CLOSE_DELAY=-1;DB_CLOSE_ON_EXIT=FALSE",
                "spring.datasource.username=sa",
                "spring.datasource.password=",
                "spring.datasource.driver-class-name=org.h2.Driver",
                "spring.jpa.hibernate.ddl-auto=create-drop",
                "spring.jpa.database-platform=org.hibernate.dialect.H2Dialect",
                // This query-only context must not run the background consumers — otherwise they'd join the
                // shared Kafka groups / Pub-Sub subscriptions / Event Hubs group and steal messages from the
                // main context's reporting tests.
                "breakfast.background-consumers.enabled=false"
        ).applyTo(context.getEnvironment());
    }
}
