package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.databind.JsonNode;
import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.List;
import java.util.Map;

/** Cucumber step definitions for the Infrastructure domain. */
public class InfrastructureSteps {

    private static final List<String> CHECKS =
            List.of("CowService", "GoatService", "SupplierService", "KitchenService", "CosmosDb", "Kafka");
    private static final List<String> DOWNSTREAM =
            List.of("CowService", "GoatService", "SupplierService", "KitchenService");

    private final ScenarioContext ctx;
    private TestResponse response;

    public InfrastructureSteps(ScenarioContext ctx) {
        this.ctx = ctx;
    }

    @When("the heartbeat endpoint is called")
    public void heartbeatCalled() {
        response = ctx.client().get("/");
    }

    @When("the health check endpoint is called")
    public void healthCalled() {
        response = ctx.client().get("/health");
    }

    @When("the menu is requested with correlation id {string}")
    public void menuWithCorrelationId(String correlationId) {
        response = ctx.client().get("/menu", Map.of("X-Correlation-Id", correlationId));
    }

    @When("the menu is requested without a correlation id")
    public void menuWithoutCorrelationId() {
        response = ctx.client().get("/menu");
    }

    @Then("the heartbeat status is {string}")
    public void heartbeatStatusIs(String status) {
        assertThat(response.status()).isEqualTo(200);
        assertThat(response.json().get("status").asText()).isEqualTo(status);
    }

    @Then("the overall health status is {string}")
    public void overallHealthStatusIs(String status) {
        assertThat(response.status()).isEqualTo(200);
        assertThat(response.json().get("status").asText()).isEqualTo(status);
    }

    @Then("the health results include all dependency checks")
    public void healthResultsIncludeAllChecks() {
        JsonNode results = response.json().get("results");
        for (String check : CHECKS) {
            assertThat(results.has(check)).as("results should include " + check).isTrue();
        }
    }

    @Then("each health entry has a status and a data object")
    public void eachHealthEntryHasStatusAndData() {
        response.json().get("results").fields().forEachRemaining(entry -> {
            assertThat(entry.getValue().get("status").asText()).isNotBlank();
            assertThat(entry.getValue().has("data")).isTrue();
        });
    }

    @Then("each downstream health entry has a description")
    public void eachDownstreamEntryHasDescription() {
        JsonNode results = response.json().get("results");
        for (String check : DOWNSTREAM) {
            assertThat(results.get(check).get("description").asText()).isNotBlank();
        }
    }

    @Then("the response echoes correlation id {string}")
    public void responseEchoesCorrelationId(String correlationId) {
        assertThat(response.header("X-Correlation-Id")).isEqualTo(correlationId);
    }

    @Then("the response contains a generated correlation id")
    public void responseContainsGeneratedCorrelationId() {
        assertThat(response.header("X-Correlation-Id")).isNotBlank();
    }
}
