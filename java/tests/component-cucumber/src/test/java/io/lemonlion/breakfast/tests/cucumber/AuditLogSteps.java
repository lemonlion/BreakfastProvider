package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.core.type.TypeReference;
import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.request.OrderItemRequest;
import io.lemonlion.breakfast.model.request.OrderRequest;
import io.lemonlion.breakfast.model.response.AuditLogResponse;
import io.lemonlion.breakfast.model.response.OrderResponse;
import java.util.List;
import java.util.UUID;

/** Cucumber step definitions for the AuditLogs domain. */
public class AuditLogSteps {

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
        List<AuditLogResponse> logs = ctx.lastResponse.as(new TypeReference<List<AuditLogResponse>>() { });
        assertThat(logs).anyMatch(l -> "Created".equals(l.action()) && "Order".equals(l.entityType()));
    }
}
