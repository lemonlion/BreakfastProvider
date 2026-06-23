package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.grpc.Status;
import io.grpc.StatusRuntimeException;
import io.lemonlion.breakfast.grpc.OrderStatusReply;
import io.lemonlion.breakfast.grpc.OrderStatusRequest;
import io.lemonlion.breakfast.grpc.RecipeSummaryReply;
import io.lemonlion.breakfast.grpc.RecipeSummaryRequest;
import io.lemonlion.breakfast.model.request.OrderItemRequest;
import io.lemonlion.breakfast.model.request.OrderRequest;
import io.lemonlion.breakfast.model.response.OrderResponse;
import io.lemonlion.breakfast.testsupport.GrpcSupport;
import java.util.ArrayList;
import java.util.List;
import java.util.UUID;

/** Cucumber step definitions for the Grpc domain. */
public class GrpcSteps {

    private final ScenarioContext ctx;

    private RecipeSummaryReply recipeReply;
    private OrderStatusReply orderReply;
    private final List<OrderStatusReply> streamedReplies = new ArrayList<>();
    private StatusRuntimeException rpcException;

    public GrpcSteps(ScenarioContext ctx) {
        this.ctx = ctx;
    }

    @When("a recipe summary is requested for {string} via grpc")
    public void aRecipeSummaryIsRequested(String recipeType) {
        recipeReply = GrpcSupport.blockingStub()
                .getRecipeSummary(RecipeSummaryRequest.newBuilder().setRecipeType(recipeType).build());
    }

    @When("an order is placed and its status is requested via grpc")
    public void anOrderIsPlacedAndStatusRequested() {
        String orderId = placeOrder();
        orderReply = GrpcSupport.blockingStub()
                .getOrderStatus(OrderStatusRequest.newBuilder().setOrderId(orderId).build());
    }

    @When("the status of a non-existent order is requested via grpc")
    public void nonExistentOrderStatus() {
        try {
            GrpcSupport.blockingStub()
                    .getOrderStatus(OrderStatusRequest.newBuilder().setOrderId(UUID.randomUUID().toString()).build());
        } catch (StatusRuntimeException ex) {
            rpcException = ex;
        }
    }

    @When("an order is placed and its updates are streamed via grpc")
    public void anOrderIsPlacedAndStreamed() {
        String orderId = placeOrder();
        GrpcSupport.blockingStub()
                .streamOrderUpdates(OrderStatusRequest.newBuilder().setOrderId(orderId).build())
                .forEachRemaining(streamedReplies::add);
    }

    @Then("the recipe summary has {int} total batches")
    public void recipeSummaryBatches(int expected) {
        assertThat(recipeReply.getTotalBatches()).isEqualTo(expected);
    }

    @Then("the common ingredients are {string}")
    public void commonIngredientsAre(String csv) {
        assertThat(recipeReply.getCommonIngredientsList()).containsExactly(csv.split(","));
    }

    @Then("the common ingredients are empty")
    public void commonIngredientsAreEmpty() {
        assertThat(recipeReply.getCommonIngredientsList()).isEmpty();
    }

    @Then("the grpc order status is {string}")
    public void grpcOrderStatusIs(String status) {
        assertThat(orderReply.getStatus()).isEqualTo(status);
    }

    @Then("the grpc response is a not found error")
    public void grpcNotFound() {
        assertThat(rpcException).isNotNull();
        assertThat(rpcException.getStatus().getCode()).isEqualTo(Status.Code.NOT_FOUND);
    }

    @Then("the streamed order status is {string}")
    public void streamedOrderStatusIs(String status) {
        assertThat(streamedReplies).hasSize(1);
        assertThat(streamedReplies.get(0).getStatus()).isEqualTo(status);
    }

    private String placeOrder() {
        String customer = "Cust-" + UUID.randomUUID();
        OrderResponse order = ctx.client().post("/orders",
                new OrderRequest(customer, List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 2)), 5))
                .as(OrderResponse.class);
        return order.orderId().toString();
    }
}
