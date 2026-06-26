package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import io.cucumber.java.en.Given;
import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.request.OrderItemRequest;
import io.lemonlion.breakfast.model.request.OrderRequest;
import io.lemonlion.breakfast.model.request.UpdateOrderStatusRequest;
import io.lemonlion.breakfast.model.response.OrderResponse;
import io.lemonlion.breakfast.testsupport.BreakfastBackends;
import java.util.List;
import java.util.UUID;

/** Cucumber step definitions for the Orders domain. Shared response state lives in {@link ScenarioContext}. */
public class OrderSteps {

    private final ScenarioContext ctx;
    private OrderRequest request;
    private OrderResponse createdOrder;

    public OrderSteps(ScenarioContext ctx) {
        this.ctx = ctx;
    }

    private static OrderRequest valid() {
        return new OrderRequest("Alice", List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 2)), 5);
    }

    @Given("a valid breakfast order")
    public void aValidBreakfastOrder() {
        request = valid();
    }

    @Given("an order request without a customer name")
    public void anOrderWithoutCustomerName() {
        request = new OrderRequest(null, List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 1)), 1);
    }

    @Given("an order request with {int} items")
    public void anOrderRequestWithItems(int count) {
        java.util.List<OrderItemRequest> items = new java.util.ArrayList<>();
        for (int i = 0; i < count; i++) {
            items.add(new OrderItemRequest("Pancakes", UUID.randomUUID(), 1));
        }
        request = new OrderRequest("Alice", items, 1);
    }

    @Given("a placed breakfast order")
    public void aPlacedBreakfastOrder() {
        createdOrder = ctx.client().post("/orders", valid()).as(OrderResponse.class);
    }

    @Given("the kitchen service is failing")
    public void theKitchenServiceIsFailing() {
        // Touch the client first so its one-time kitchen reset happens before we force a failure.
        ctx.client();
        BreakfastBackends.kitchen().setNextStatus(503);
    }

    @Given("two breakfast orders have been placed")
    public void twoOrdersHaveBeenPlaced() {
        String customer = "Page-" + UUID.randomUUID();
        ctx.client().post("/orders", new OrderRequest(customer, List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 1)), 1));
        ctx.client().post("/orders", new OrderRequest(customer, List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 1)), 2));
    }

    @When("orders are listed with page {int} and page size {int}")
    public void ordersAreListed(int page, int pageSize) {
        ctx.lastResponse = ctx.client().get("/orders?page=" + page + "&pageSize=" + pageSize);
    }

    @Then("the pagination metadata reflects page {int} with page size {int}")
    public void paginationMetadataReflects(int page, int pageSize) {
        com.fasterxml.jackson.databind.JsonNode body = ctx.lastResponse.json();
        assertThat(body.get("page").asInt()).isEqualTo(page);
        assertThat(body.get("pageSize").asInt()).isEqualTo(pageSize);
        assertThat(body.get("items").size()).isEqualTo(pageSize);
        assertThat(body.get("totalCount").asInt()).isGreaterThanOrEqualTo(2);
    }

    @When("the order is placed")
    public void theOrderIsPlaced() {
        ctx.lastResponse = ctx.client().post("/orders", request);
        if (ctx.lastResponse.status() == 201) {
            createdOrder = ctx.lastResponse.as(OrderResponse.class);
        }
    }

    @When("a missing order is retrieved")
    public void aMissingOrderIsRetrieved() {
        ctx.lastResponse = ctx.client().get("/orders/" + UUID.randomUUID());
    }

    @When("the order status is updated to {string}")
    public void theOrderStatusIsUpdatedTo(String status) {
        ctx.lastResponse = ctx.client().patch("/orders/" + createdOrder.orderId() + "/status",
                new UpdateOrderStatusRequest(status));
    }

    @Then("the order is created successfully")
    public void theOrderIsCreatedSuccessfully() {
        assertThat(ctx.lastResponse.status()).isEqualTo(201);
        assertThat(createdOrder.status()).isEqualTo("Created");
    }

    @Then("the kitchen receives a preparation request")
    public void theKitchenReceivesAPreparationRequest() {
        assertThat(BreakfastBackends.kitchen().receivedPreparation()).isTrue();
    }

    @Then("the order status is {string}")
    public void theOrderStatusIs(String status) {
        assertThat(ctx.lastResponse.as(OrderResponse.class).status()).isEqualTo(status);
    }

    @Then("a Created audit log entry exists for the order")
    public void aCreatedAuditLogEntryExists() {
        var audit = ctx.client().get("/audit-logs?entityType=Order&entityId=" + createdOrder.orderId());
        assertThat(audit.status()).isEqualTo(200);
        assertThat(audit.bodyContains("Created")).isTrue();
        assertThat(audit.bodyContains(createdOrder.orderId().toString())).isTrue();
    }
}
