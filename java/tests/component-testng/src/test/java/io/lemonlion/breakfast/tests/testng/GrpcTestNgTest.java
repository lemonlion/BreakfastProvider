package io.lemonlion.breakfast.tests.testng;

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
import java.util.ArrayList;
import java.util.List;
import java.util.UUID;
import org.testng.annotations.Test;

/** Grpc domain component tests (TestNG) — twin of the C# BreakfastGrpcService scenarios. */
public class GrpcTestNgTest extends ComponentTestBaseNg {

    @Test
    public void recipeSummaryPancakes() {
        RecipeSummaryReply reply = GrpcSupport.blockingStub()
                .getRecipeSummary(RecipeSummaryRequest.newBuilder().setRecipeType("Pancakes").build());

        assertThat(reply.getRecipeType()).isEqualTo("Pancakes");
        assertThat(reply.getTotalBatches()).isEqualTo(42);
        assertThat(reply.getCommonIngredientsList()).containsExactly("Milk", "Flour", "Eggs");
    }

    @Test
    public void recipeSummaryWaffles() {
        RecipeSummaryReply reply = GrpcSupport.blockingStub()
                .getRecipeSummary(RecipeSummaryRequest.newBuilder().setRecipeType("Waffles").build());

        assertThat(reply.getRecipeType()).isEqualTo("Waffles");
        assertThat(reply.getTotalBatches()).isEqualTo(28);
        assertThat(reply.getCommonIngredientsList()).containsExactly("Milk", "Flour", "Eggs", "Butter");
    }

    @Test
    public void recipeSummaryUnknown() {
        RecipeSummaryReply reply = GrpcSupport.blockingStub()
                .getRecipeSummary(RecipeSummaryRequest.newBuilder().setRecipeType("Unknown").build());

        assertThat(reply.getRecipeType()).isEqualTo("Unknown");
        assertThat(reply.getTotalBatches()).isZero();
        assertThat(reply.getCommonIngredientsList()).isEmpty();
    }

    @Test
    public void orderStatusForCreatedOrder() {
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
    public void orderStatusNotFound() {
        assertThatThrownBy(() -> GrpcSupport.blockingStub()
                .getOrderStatus(OrderStatusRequest.newBuilder().setOrderId(UUID.randomUUID().toString()).build()))
                .isInstanceOf(StatusRuntimeException.class)
                .satisfies(ex -> assertThat(((StatusRuntimeException) ex).getStatus().getCode())
                        .isEqualTo(Status.Code.NOT_FOUND));
    }

    @Test
    public void streamOrderUpdates() {
        String customer = "Cust-" + UUID.randomUUID();
        OrderResponse order = client.post("/orders",
                new OrderRequest(customer, List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 1)), 3))
                .as(OrderResponse.class);

        List<OrderStatusReply> replies = new ArrayList<>();
        GrpcSupport.blockingStub()
                .streamOrderUpdates(OrderStatusRequest.newBuilder().setOrderId(order.orderId().toString()).build())
                .forEachRemaining(replies::add);

        assertThat(replies).hasSize(1);
        assertThat(replies.get(0).getOrderId()).isEqualTo(order.orderId().toString());
        assertThat(replies.get(0).getStatus()).isEqualTo("Created");
    }
}
