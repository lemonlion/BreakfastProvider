package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;

import ch.qos.logback.classic.Logger;
import ch.qos.logback.classic.spi.ILoggingEvent;
import ch.qos.logback.core.read.ListAppender;
import com.fasterxml.jackson.databind.JsonNode;
import io.lemonlion.breakfast.model.request.OrderItemRequest;
import io.lemonlion.breakfast.model.request.OrderRequest;
import io.lemonlion.breakfast.testsupport.BreakfastBackends;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.slf4j.LoggerFactory;

/** Infrastructure component tests (JUnit 5): heartbeat, health checks, correlation id. */
@DisplayName("Infrastructure")
class InfrastructureComponentTest extends ComponentTestBase {

    private static final List<String> CHECKS =
            List.of("CowService", "GoatService", "SupplierService", "KitchenService", "CosmosDb", "Kafka");
    private static final List<String> DOWNSTREAM =
            List.of("CowService", "GoatService", "SupplierService", "KitchenService");

    @Test
    @DisplayName("the heartbeat endpoint reports the service is running")
    void heartbeat() {
        TestResponse response = client.get("/");

        assertThat(response.status()).isEqualTo(200);
        assertThat(response.json().get("status").asText()).isEqualTo("ok");
    }

    @Test
    @DisplayName("the health check reports healthy with all dependencies")
    void healthCheckHealthy() {
        TestResponse response = client.get("/health");

        assertThat(response.status()).isEqualTo(200);
        JsonNode body = response.json();
        assertThat(body.get("status").asText()).isEqualTo("Healthy");
        JsonNode results = body.get("results");
        for (String check : CHECKS) {
            assertThat(results.has(check)).as("results should include " + check).isTrue();
        }
    }

    @Test
    @DisplayName("the health check response contains detailed entries")
    void healthCheckDetail() {
        JsonNode results = client.get("/health").json().get("results");

        results.fields().forEachRemaining(entry -> {
            assertThat(entry.getValue().get("status").asText()).isNotBlank();
            assertThat(entry.getValue().has("data")).isTrue();
        });
        for (String check : DOWNSTREAM) {
            assertThat(results.get(check).get("description").asText()).isNotBlank();
        }
    }

    @Test
    @DisplayName("a known correlation id is echoed back on the response")
    void correlationIdEchoed() {
        String correlationId = UUID.randomUUID().toString();

        TestResponse response = client.get("/menu", Map.of("X-Correlation-Id", correlationId));

        assertThat(response.header("X-Correlation-Id")).isEqualTo(correlationId);
    }

    @Test
    @DisplayName("a correlation id is generated when the request omits one")
    void correlationIdGenerated() {
        TestResponse response = client.get("/menu");

        assertThat(response.header("X-Correlation-Id")).isNotBlank();
    }

    @Test
    @DisplayName("the correlation id is propagated to downstream services")
    void correlationIdPropagatedDownstream() {
        String correlationId = UUID.randomUUID().toString();

        client.get("/milk", Map.of("X-Correlation-Id", correlationId));

        assertThat(BreakfastBackends.cow().lastCorrelationId()).isEqualTo(correlationId);
    }

    @Test
    @DisplayName("a structured log entry is captured for order creation")
    void telemetryCapturesOrderCreationLog() {
        Logger root = (Logger) LoggerFactory.getLogger(org.slf4j.Logger.ROOT_LOGGER_NAME);
        ListAppender<ILoggingEvent> appender = new ListAppender<>();
        appender.start();
        root.addAppender(appender);
        try {
            String customer = "Telemetry-" + UUID.randomUUID();
            client.post("/orders",
                    new OrderRequest(customer, List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 1)), 1));

            assertThat(appender.list).anyMatch(e -> {
                String msg = e.getFormattedMessage();
                return msg.contains("created for customer") && msg.contains(customer) && msg.contains("1 items");
            });
        } finally {
            root.detachAppender(appender);
        }
    }

    @Test
    @DisplayName("the health check reports degraded when downstream services are unreachable")
    void degradedHealthWhenDownstreamUnreachable() {
        BreakfastBackends.cow().setHealthStatus(503);
        BreakfastBackends.supplier().setHealthStatus(503);

        JsonNode body = client.get("/health").json();

        assertThat(body.get("status").asText()).isEqualTo("Degraded");
        assertThat(body.get("results").get("CowService").get("status").asText()).isEqualTo("Degraded");
        assertThat(body.get("results").get("SupplierService").get("status").asText()).isEqualTo("Degraded");
    }

    @Test
    @DisplayName("the health check reports degraded when a downstream health endpoint errors")
    void downstreamErrorHealthWhenKitchenFails() {
        BreakfastBackends.kitchen().setHealthStatus(503);

        JsonNode body = client.get("/health").json();

        assertThat(body.get("status").asText()).isEqualTo("Degraded");
        assertThat(body.get("results").get("KitchenService").get("status").asText()).isEqualTo("Degraded");
    }
}
