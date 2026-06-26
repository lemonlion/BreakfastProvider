package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

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
import java.util.List;
import java.util.UUID;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

/** Grpc domain component tests (JUnit 5) — twin of the C# BreakfastGrpcService scenarios. */
@DisplayName("Grpc")
class GrpcComponentTest extends ComponentTestBase {

    @Test
    @DisplayName("recipe summary for Pancakes returns 42 batches and its ingredients")
    void recipeSummaryPancakes() {
        RecipeSummaryReply reply = GrpcSupport.blockingStub()
                .getRecipeSummary(RecipeSummaryRequest.newBuilder().setRecipeType("Pancakes").build());

        assertThat(reply.getRecipeType()).isEqualTo("Pancakes");
        assertThat(reply.getTotalBatches()).isEqualTo(42);
        assertThat(reply.getCommonIngredientsList()).containsExactly("Milk", "Flour", "Eggs");
    }

    @Test
    @DisplayName("recipe summary for Waffles returns 28 batches and its ingredients")
    void recipeSummaryWaffles() {
        RecipeSummaryReply reply = GrpcSupport.blockingStub()
                .getRecipeSummary(RecipeSummaryRequest.newBuilder().setRecipeType("Waffles").build());

        assertThat(reply.getRecipeType()).isEqualTo("Waffles");
        assertThat(reply.getTotalBatches()).isEqualTo(28);
        assertThat(reply.getCommonIngredientsList()).containsExactly("Milk", "Flour", "Eggs", "Butter");
    }

    @Test
    @DisplayName("recipe summary for an unknown type returns zero batches and no ingredients")
    void recipeSummaryUnknown() {
        RecipeSummaryReply reply = GrpcSupport.blockingStub()
                .getRecipeSummary(RecipeSummaryRequest.newBuilder().setRecipeType("Unknown").build());

        assertThat(reply.getRecipeType()).isEqualTo("Unknown");
        assertThat(reply.getTotalBatches()).isZero();
        assertThat(reply.getCommonIngredientsList()).isEmpty();
    }

    @Test
    @DisplayName("order status returns the created order's details")
    void orderStatusForCreatedOrder() {
        String customer = "Cust-" + UUID.randomUUID();
        OrderResponse order = client.post("/orders",
                new OrderRequest(customer, List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 2)), 5))
                .as(OrderResponse.class);

        OrderStatusReply reply = GrpcSupport.blockingStub()
                .getOrderStatus(OrderStatusRequest.newBuilder().setOrderId(order.orderId().toString()).build());

        assertThat(reply.getOrderId()).isEqualTo(order.orderId().toString());
        assertThat(reply.getCustomerName()).isEqualTo(customer);
        assertThat(reply.getStatus()).isEqualTo("Created");
        assertThat(reply.getItemCount()).isEqualTo(1);
    }

    @Test
    @DisplayName("order status for a non-existent order is a NOT_FOUND error")
    void orderStatusNotFound() {
        assertThatThrownBy(() -> GrpcSupport.blockingStub()
                .getOrderStatus(OrderStatusRequest.newBuilder().setOrderId(UUID.randomUUID().toString()).build()))
                .isInstanceOf(StatusRuntimeException.class)
                .satisfies(ex -> assertThat(((StatusRuntimeException) ex).getStatus().getCode())
                        .isEqualTo(Status.Code.NOT_FOUND));
    }

    @Test
    @DisplayName("stream order updates emits the current status for an existing order")
    void streamOrderUpdates() {
        String customer = "Cust-" + UUID.randomUUID();
        OrderResponse order = client.post("/orders",
                new OrderRequest(customer, List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 1)), 3))
                .as(OrderResponse.class);

        List<OrderStatusReply> replies = new java.util.ArrayList<>();
        GrpcSupport.blockingStub()
                .streamOrderUpdates(OrderStatusRequest.newBuilder().setOrderId(order.orderId().toString()).build())
                .forEachRemaining(replies::add);

        assertThat(replies).hasSize(1);
        assertThat(replies.get(0).getOrderId()).isEqualTo(order.orderId().toString());
        assertThat(replies.get(0).getStatus()).isEqualTo("Created");
    }

    @Test
    @DisplayName("stream order updates for a non-existent order is a NOT_FOUND error")
    void streamOrderUpdatesNotFound() {
        assertThatThrownBy(() -> GrpcSupport.blockingStub()
                .streamOrderUpdates(OrderStatusRequest.newBuilder().setOrderId(UUID.randomUUID().toString()).build())
                .forEachRemaining(reply -> { }))
                .isInstanceOf(StatusRuntimeException.class)
                .satisfies(ex -> assertThat(((StatusRuntimeException) ex).getStatus().getCode())
                        .isEqualTo(Status.Code.NOT_FOUND));
    }
}
