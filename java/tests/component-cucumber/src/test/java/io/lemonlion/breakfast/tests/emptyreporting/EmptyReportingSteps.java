package io.lemonlion.breakfast.tests.emptyreporting;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.databind.JsonNode;
import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.testsupport.BreakfastTestClient;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.Map;
import org.springframework.core.env.Environment;

/** Cucumber glue for the isolated order-summaries-empty context. */
public class EmptyReportingSteps {

    private final Environment environment;
    private BreakfastTestClient client;
    private TestResponse response;

    public EmptyReportingSteps(Environment environment) {
        this.environment = environment;
    }

    private BreakfastTestClient client() {
        if (client == null) {
            client = new BreakfastTestClient("http://127.0.0.1:" + environment.getProperty("local.server.port"));
        }
        return client;
    }

    @When("the order summaries are queried with no orders present")
    public void theOrderSummariesAreQueried() {
        response = client().post("/graphql", Map.of("query", "{ orderSummaries { orderId } }"));
    }

    @Then("the order summaries list is empty")
    public void theOrderSummariesListIsEmpty() {
        assertThat(response.status()).isEqualTo(200);
        JsonNode summaries = response.json().get("data").get("orderSummaries");
        assertThat(summaries.isArray()).isTrue();
        assertThat(summaries.size()).isZero();
    }
}
