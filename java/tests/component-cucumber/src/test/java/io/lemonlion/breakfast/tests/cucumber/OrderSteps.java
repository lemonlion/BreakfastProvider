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

    @Given("a placed breakfast order")
    public void aPlacedBreakfastOrder() {
        createdOrder = ctx.client().post("/orders", valid()).as(OrderResponse.class);
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
}
