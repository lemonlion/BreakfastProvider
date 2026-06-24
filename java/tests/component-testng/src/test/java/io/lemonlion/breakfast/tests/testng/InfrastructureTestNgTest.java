package io.lemonlion.breakfast.tests.testng;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.databind.JsonNode;
import io.lemonlion.breakfast.testsupport.BreakfastBackends;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import org.testng.annotations.Test;

/** Infrastructure component tests (TestNG): heartbeat, health checks, correlation id. */
public class InfrastructureTestNgTest extends ComponentTestBaseNg {

    private static final List<String> CHECKS =
            List.of("CowService", "GoatService", "SupplierService", "KitchenService", "CosmosDb", "Kafka");
    private static final List<String> DOWNSTREAM =
            List.of("CowService", "GoatService", "SupplierService", "KitchenService");

    @Test
    public void heartbeat() {
        TestResponse response = client.get("/");

        assertThat(response.status()).isEqualTo(200);
        assertThat(response.json().get("status").asText()).isEqualTo("ok");
    }

    @Test
    public void healthCheckHealthy() {
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
    public void healthCheckDetail() {
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
    public void correlationIdEchoed() {
        String correlationId = UUID.randomUUID().toString();

        TestResponse response = client.get("/menu", Map.of("X-Correlation-Id", correlationId));

        assertThat(response.header("X-Correlation-Id")).isEqualTo(correlationId);
    }

    @Test
    public void correlationIdGenerated() {
        TestResponse response = client.get("/menu");

        assertThat(response.header("X-Correlation-Id")).isNotBlank();
    }

    @Test
    public void correlationIdPropagatedDownstream() {
        String correlationId = UUID.randomUUID().toString();

        client.get("/milk", Map.of("X-Correlation-Id", correlationId));

        assertThat(BreakfastBackends.cow().lastCorrelationId()).isEqualTo(correlationId);
    }
}
