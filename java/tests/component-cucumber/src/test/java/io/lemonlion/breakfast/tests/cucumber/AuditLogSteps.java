package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.core.type.TypeReference;
import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.request.OrderItemRequest;
import io.lemonlion.breakfast.model.request.OrderRequest;
import io.lemonlion.breakfast.model.response.AuditLogResponse;
import io.lemonlion.breakfast.model.response.OrderResponse;
import java.util.Comparator;
import java.util.List;
import java.util.UUID;

/** Cucumber step definitions for the AuditLogs domain. */
public class AuditLogSteps {

    private static final TypeReference<List<AuditLogResponse>> LOGS = new TypeReference<>() { };

    private final ScenarioContext ctx;
    private OrderResponse order;

    public AuditLogSteps(ScenarioContext ctx) {
        this.ctx = ctx;
    }

    @When("an order is placed and its audit log is queried")
    public void anOrderIsPlacedAndAuditQueried() {
        order = ctx.client().post("/orders",
                        new OrderRequest("Alice", List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 1)), 3))
                .as(OrderResponse.class);
        ctx.lastResponse = ctx.client().get("/audit-logs?entityId=" + order.orderId());
    }

    @Then("the audit log records the order creation")
    public void theAuditLogRecordsCreation() {
        assertThat(ctx.lastResponse.status()).isEqualTo(200);
        List<AuditLogResponse> logs = ctx.lastResponse.as(LOGS);
        assertThat(logs).anyMatch(l -> "Created".equals(l.action()) && "Order".equals(l.entityType()));
    }

    @When("an order is placed and audit logs are filtered by entity type")
    public void anOrderIsPlacedAndFilteredByEntityType() {
        ctx.client().post("/orders",
                new OrderRequest("Bob", List.of(new OrderItemRequest("Waffles", UUID.randomUUID(), 1)), 4));
        ctx.lastResponse = ctx.client().get("/audit-logs?entityType=Order");
    }

    @Then("every audit log entry is of type {string}")
    public void everyAuditLogEntryIsOfType(String entityType) {
        assertThat(ctx.lastResponse.status()).isEqualTo(200);
        assertThat(ctx.lastResponse.as(LOGS)).isNotEmpty().allMatch(l -> entityType.equals(l.entityType()));
    }

    @When("an order is placed and audit logs are filtered by its entity id")
    public void anOrderIsPlacedAndFilteredByEntityId() {
        order = ctx.client().post("/orders",
                        new OrderRequest("Carol", List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 1)), 5))
                .as(OrderResponse.class);
        ctx.lastResponse = ctx.client().get("/audit-logs?entityId=" + order.orderId());
    }

    @Then("every audit log entry is for that order")
    public void everyAuditLogEntryIsForThatOrder() {
        assertThat(ctx.lastResponse.status()).isEqualTo(200);
        assertThat(ctx.lastResponse.as(LOGS)).isNotEmpty().allMatch(l -> order.orderId().equals(l.entityId()));
    }

    @When("audit logs are filtered by a non-existent entity type")
    public void auditLogsFilteredByNonExistentType() {
        ctx.lastResponse = ctx.client().get("/audit-logs?entityType=NonExistent_" + UUID.randomUUID());
    }

    @Then("the audit log collection is empty")
    public void theAuditLogCollectionIsEmpty() {
        assertThat(ctx.lastResponse.status()).isEqualTo(200);
        assertThat(ctx.lastResponse.as(LOGS)).isEmpty();
    }

    @When("two orders are placed and audit logs are queried")
    public void twoOrdersArePlacedAndAuditLogsQueried() {
        ctx.client().post("/orders",
                new OrderRequest("Dave", List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 1)), 6));
        ctx.client().post("/orders",
                new OrderRequest("Erin", List.of(new OrderItemRequest("Waffles", UUID.randomUUID(), 1)), 7));
        ctx.lastResponse = ctx.client().get("/audit-logs?entityType=Order");
    }

    @Then("the audit logs are in descending timestamp order")
    public void theAuditLogsAreInDescendingTimestampOrder() {
        assertThat(ctx.lastResponse.status()).isEqualTo(200);
        List<AuditLogResponse> logs = ctx.lastResponse.as(LOGS);
        assertThat(logs).isNotEmpty();
        assertThat(logs).extracting(AuditLogResponse::timestamp).isSortedAccordingTo(Comparator.reverseOrder());
    }
}
