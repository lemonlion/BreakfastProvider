package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.databind.JsonNode;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.List;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

/** Specifications component tests (JUnit 5): OpenAPI document, Scalar UI, AsyncAPI document. */
@DisplayName("Specifications")
class SpecificationsComponentTest extends ComponentTestBase {

    private static final List<String> EXPECTED_PATHS = List.of(
            "/pancakes", "/waffles", "/orders", "/orders/{orderId}", "/toppings",
            "/menu", "/milk", "/eggs", "/flour", "/goat-milk", "/audit-logs");

    private static final List<String> ASYNCAPI_KEYS =
            List.of("asyncapi", "info", "defaultContentType", "channels", "operations", "components");

    @Test
    @DisplayName("the OpenAPI document is valid and contains all the endpoints")
    void openApiContainsEndpoints() {
        TestResponse response = client.get("/openapi/v1.json");

        assertThat(response.status()).isEqualTo(200);
        JsonNode paths = response.json().get("paths");
        for (String path : EXPECTED_PATHS) {
            assertThat(paths.has(path)).as("OpenAPI paths should contain " + path).isTrue();
        }
    }

    @Test
    @DisplayName("the Scalar UI endpoint returns a valid Scalar page")
    void scalarUi() {
        TestResponse response = client.get("/scalar/v1", java.util.Map.of("Accept", "text/html"));

        assertThat(response.status()).isEqualTo(200);
        assertThat(response.body()).contains("<html");
        assertThat(response.body().toLowerCase()).contains("scalar");
    }

    @Test
    @DisplayName("the AsyncAPI document is valid and contains the expected top-level sections")
    void asyncApiDocument() {
        TestResponse response = client.get("/asyncapi/v1.json");

        assertThat(response.status()).isEqualTo(200);
        JsonNode doc = response.json();
        for (String key : ASYNCAPI_KEYS) {
            assertThat(doc.has(key)).as("AsyncAPI document should contain " + key).isTrue();
        }
    }
}
