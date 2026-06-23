package io.lemonlion.breakfast.tests.testng;

import static org.assertj.core.api.Assertions.assertThat;

import io.lemonlion.breakfast.model.request.OrderItemRequest;
import io.lemonlion.breakfast.model.request.OrderRequest;
import io.lemonlion.breakfast.model.request.UpdateOrderStatusRequest;
import io.lemonlion.breakfast.model.response.OrderResponse;
import io.lemonlion.breakfast.testsupport.BreakfastBackends;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.List;
import java.util.UUID;
import org.testng.annotations.Test;

/** Orders domain component tests (TestNG) — same behaviour as the JUnit 5 suite, different framework. */
public class OrdersTestNgTest extends ComponentTestBaseNg {

    private static OrderRequest validOrder() {
        return new OrderRequest("Alice", List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 2)), 5);
    }

    @Test
    public void createValidOrder() {
        TestResponse response = client.post("/orders", validOrder());

        assertThat(response.status()).isEqualTo(201);
        OrderResponse order = response.as(OrderResponse.class);
        assertThat(order.customerName()).isEqualTo("Alice");
        assertThat(order.status()).isEqualTo("Created");
        assertThat(BreakfastBackends.kitchen().receivedPreparation()).isTrue();

        TestResponse fetched = client.get("/orders/" + order.orderId());
        assertThat(fetched.status()).isEqualTo(200);
    }

    @Test
    public void getMissingOrder() {
        assertThat(client.get("/orders/" + UUID.randomUUID()).status()).isEqualTo(404);
    }

    @Test
    public void validStatusTransition() {
        OrderResponse order = client.post("/orders", validOrder()).as(OrderResponse.class);
        TestResponse updated = client.patch("/orders/" + order.orderId() + "/status",
                new UpdateOrderStatusRequest("Preparing"));
        assertThat(updated.status()).isEqualTo(200);
        assertThat(updated.as(OrderResponse.class).status()).isEqualTo("Preparing");
    }

    @Test
    public void invalidStatusTransition() {
        OrderResponse order = client.post("/orders", validOrder()).as(OrderResponse.class);
        TestResponse updated = client.patch("/orders/" + order.orderId() + "/status",
                new UpdateOrderStatusRequest("Ready"));
        assertThat(updated.status()).isEqualTo(409);
    }

    @Test
    public void validationRejectsMissingCustomer() {
        OrderRequest invalid = new OrderRequest(null, List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 1)), 1);
        TestResponse response = client.post("/orders", invalid);
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Customer Name' is required.")).isTrue();
    }
}
