package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.databind.JsonNode;
import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.List;
import java.util.Map;

/** Cucumber step definitions for the Specifications domain. */
public class SpecificationsSteps {

    private static final List<String> EXPECTED_PATHS = List.of(
            "/pancakes", "/waffles", "/orders", "/orders/{orderId}", "/toppings",
            "/menu", "/milk", "/eggs", "/flour", "/goat-milk", "/audit-logs");
    private static final List<String> ASYNCAPI_KEYS =
            List.of("asyncapi", "info", "defaultContentType", "channels", "operations", "components");

    private final ScenarioContext ctx;
    private TestResponse response;

    public SpecificationsSteps(ScenarioContext ctx) {
        this.ctx = ctx;
    }

    @When("the OpenAPI document is requested")
    public void openApiRequested() {
        response = ctx.client().get("/openapi/v1.json");
    }

    @When("the Scalar UI is requested")
    public void scalarRequested() {
        response = ctx.client().get("/scalar/v1", Map.of("Accept", "text/html"));
    }

    @When("the AsyncAPI document is requested")
    public void asyncApiRequested() {
        response = ctx.client().get("/asyncapi/v1.json");
    }

    @Then("the OpenAPI paths include all the breakfast endpoints")
    public void openApiPathsIncludeAll() {
        assertThat(response.status()).isEqualTo(200);
        JsonNode paths = response.json().get("paths");
        for (String path : EXPECTED_PATHS) {
            assertThat(paths.has(path)).as("OpenAPI paths should contain " + path).isTrue();
        }
    }

    @Then("the response is a Scalar HTML page")
    public void responseIsScalarPage() {
        assertThat(response.status()).isEqualTo(200);
        assertThat(response.body()).contains("<html");
        assertThat(response.body().toLowerCase()).contains("scalar");
    }

    @Then("the AsyncAPI document contains the expected top-level sections")
    public void asyncApiContainsSections() {
        assertThat(response.status()).isEqualTo(200);
        JsonNode doc = response.json();
        for (String key : ASYNCAPI_KEYS) {
            assertThat(doc.has(key)).as("AsyncAPI document should contain " + key).isTrue();
        }
    }
}
