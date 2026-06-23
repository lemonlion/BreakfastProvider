package io.lemonlion.breakfast.tests.testng;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.databind.JsonNode;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.List;
import java.util.Map;
import org.testng.annotations.Test;

/** Specifications component tests (TestNG): OpenAPI document, Scalar UI, AsyncAPI document. */
public class SpecificationsTestNgTest extends ComponentTestBaseNg {

    private static final List<String> EXPECTED_PATHS = List.of(
            "/pancakes", "/waffles", "/orders", "/orders/{orderId}", "/toppings",
            "/menu", "/milk", "/eggs", "/flour", "/goat-milk", "/audit-logs");

    private static final List<String> ASYNCAPI_KEYS =
            List.of("asyncapi", "info", "defaultContentType", "channels", "operations", "components");

    @Test
    public void openApiContainsEndpoints() {
        TestResponse response = client.get("/openapi/v1.json");

        assertThat(response.status()).isEqualTo(200);
        JsonNode paths = response.json().get("paths");
        for (String path : EXPECTED_PATHS) {
            assertThat(paths.has(path)).as("OpenAPI paths should contain " + path).isTrue();
        }
    }

    @Test
    public void scalarUi() {
        TestResponse response = client.get("/scalar/v1", Map.of("Accept", "text/html"));

        assertThat(response.status()).isEqualTo(200);
        assertThat(response.body()).contains("<html");
        assertThat(response.body().toLowerCase()).contains("scalar");
    }

    @Test
    public void asyncApiDocument() {
        TestResponse response = client.get("/asyncapi/v1.json");

        assertThat(response.status()).isEqualTo(200);
        JsonNode doc = response.json();
        for (String key : ASYNCAPI_KEYS) {
            assertThat(doc.has(key)).as("AsyncAPI document should contain " + key).isTrue();
        }
    }
}
