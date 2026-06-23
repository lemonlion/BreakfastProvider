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
import io.lemonlion.breakfast.testsupport.BreakfastTestClient;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.List;
import java.util.UUID;
import org.springframework.boot.test.web.server.LocalServerPort;

/** Cucumber step definitions for the Orders domain. Scenario-scoped (cucumber-glue) so fields reset per scenario. */
public class OrderSteps {

    @LocalServerPort
    int port;

    private BreakfastTestClient client;
    private OrderRequest request;
    private OrderResponse createdOrder;
    private TestResponse lastResponse;

    private BreakfastTestClient client() {
        if (client == null) {
            client = new BreakfastTestClient("http://127.0.0.1:" + port);
            BreakfastBackends.kitchen().reset();
        }
        return client;
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
        createdOrder = client().post("/orders", valid()).as(OrderResponse.class);
    }

    @When("the order is placed")
    public void theOrderIsPlaced() {
        lastResponse = client().post("/orders", request);
        if (lastResponse.status() == 201) {
            createdOrder = lastResponse.as(OrderResponse.class);
        }
    }

    @When("a missing order is retrieved")
    public void aMissingOrderIsRetrieved() {
        lastResponse = client().get("/orders/" + UUID.randomUUID());
    }

    @When("the order status is updated to {string}")
    public void theOrderStatusIsUpdatedTo(String status) {
        lastResponse = client().patch("/orders/" + createdOrder.orderId() + "/status",
                new UpdateOrderStatusRequest(status));
    }

    @Then("the order is created successfully")
    public void theOrderIsCreatedSuccessfully() {
        assertThat(lastResponse.status()).isEqualTo(201);
        assertThat(createdOrder.status()).isEqualTo("Created");
    }

    @Then("the kitchen receives a preparation request")
    public void theKitchenReceivesAPreparationRequest() {
        assertThat(BreakfastBackends.kitchen().receivedPreparation()).isTrue();
    }

    @Then("the response status is {int}")
    public void theResponseStatusIs(int code) {
        assertThat(lastResponse.status()).isEqualTo(code);
    }

    @Then("the order status is {string}")
    public void theOrderStatusIs(String status) {
        assertThat(lastResponse.as(OrderResponse.class).status()).isEqualTo(status);
    }

    @Then("the error mentions {string}")
    public void theErrorMentions(String message) {
        assertThat(lastResponse.bodyContains(message)).isTrue();
    }
}
