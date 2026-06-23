package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.databind.JsonNode;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

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
}
