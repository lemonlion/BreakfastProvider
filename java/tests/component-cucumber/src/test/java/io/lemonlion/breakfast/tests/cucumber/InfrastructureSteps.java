package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import ch.qos.logback.classic.Logger;
import ch.qos.logback.classic.spi.ILoggingEvent;
import ch.qos.logback.core.read.ListAppender;
import com.fasterxml.jackson.databind.JsonNode;
import io.cucumber.java.en.Given;
import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.request.OrderItemRequest;
import io.lemonlion.breakfast.model.request.OrderRequest;
import io.lemonlion.breakfast.testsupport.BreakfastBackends;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import org.slf4j.LoggerFactory;

/** Cucumber step definitions for the Infrastructure domain. */
public class InfrastructureSteps {

    private static final List<String> CHECKS =
            List.of("CowService", "GoatService", "SupplierService", "KitchenService", "CosmosDb", "Kafka");
    private static final List<String> DOWNSTREAM =
            List.of("CowService", "GoatService", "SupplierService", "KitchenService");

    private final ScenarioContext ctx;
    private TestResponse response;
    private Logger telemetryRoot;
    private ListAppender<ILoggingEvent> telemetryAppender;
    private String telemetryCustomer;

    public InfrastructureSteps(ScenarioContext ctx) {
        this.ctx = ctx;
    }

    @Given("the cow and supplier services are unreachable")
    public void cowAndSupplierUnreachable() {
        ctx.client();
        BreakfastBackends.cow().setHealthStatus(503);
        BreakfastBackends.supplier().setHealthStatus(503);
    }

    @Given("the kitchen health endpoint is failing")
    public void kitchenHealthFailing() {
        ctx.client();
        BreakfastBackends.kitchen().setHealthStatus(503);
    }

    @When("an order is placed for telemetry capture")
    public void orderPlacedForTelemetry() {
        telemetryRoot = (Logger) LoggerFactory.getLogger(org.slf4j.Logger.ROOT_LOGGER_NAME);
        telemetryAppender = new ListAppender<>();
        telemetryAppender.start();
        telemetryRoot.addAppender(telemetryAppender);
        telemetryCustomer = "Telemetry-" + UUID.randomUUID();
        ctx.client().post("/orders",
                new OrderRequest(telemetryCustomer, List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 1)), 1));
    }

    @Then("a structured order-creation log entry is captured")
    public void structuredLogCaptured() {
        try {
            assertThat(telemetryAppender.list).anyMatch(e -> {
                String msg = e.getFormattedMessage();
                return msg.contains("created for customer") && msg.contains(telemetryCustomer) && msg.contains("1 items");
            });
        } finally {
            telemetryRoot.detachAppender(telemetryAppender);
        }
    }

    @Then("the {string} health entry is {string}")
    public void healthEntryIs(String name, String status) {
        assertThat(response.json().get("results").get(name).get("status").asText()).isEqualTo(status);
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

    @When("milk is requested with correlation id {string}")
    public void milkWithCorrelationId(String correlationId) {
        response = ctx.client().get("/milk", Map.of("X-Correlation-Id", correlationId));
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

    @Then("the cow service received correlation id {string}")
    public void cowReceivedCorrelationId(String correlationId) {
        assertThat(BreakfastBackends.cow().lastCorrelationId()).isEqualTo(correlationId);
    }
}
