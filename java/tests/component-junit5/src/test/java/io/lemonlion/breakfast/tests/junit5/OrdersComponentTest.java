package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;

import io.lemonlion.breakfast.events.outbox.OutboxStore;
import io.lemonlion.breakfast.model.request.OrderItemRequest;
import io.lemonlion.breakfast.model.request.OrderRequest;
import io.lemonlion.breakfast.model.request.UpdateOrderStatusRequest;
import io.lemonlion.breakfast.model.response.OrderResponse;
import io.lemonlion.breakfast.storage.OutboxMessage;
import io.lemonlion.breakfast.testsupport.BreakfastBackends;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.List;
import java.util.UUID;
import org.awaitility.Awaitility;
import java.time.Duration;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;

/**
 * Orders domain component tests (JUnit 5). Drives the real SUT over HTTP against Cosmos + Kafka in
 * Testcontainers, so Kronikol4J captures the create → Cosmos → outbox → kitchen interactions.
 */
@DisplayName("Orders")
class OrdersComponentTest extends ComponentTestBase {

    @Autowired
    OutboxStore outboxStore;

    private static OrderRequest validOrder() {
        return new OrderRequest("Alice", List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 2)), 5);
    }

    @Test
    @DisplayName("a valid order is created, persisted, notifies the kitchen, and writes an outbox event")
    void createValidOrder() {
        TestResponse response = client.post("/orders", validOrder());

        assertThat(response.status()).isEqualTo(201);
        OrderResponse order = response.as(OrderResponse.class);
        assertThat(order.orderId()).isNotNull();
        assertThat(order.customerName()).isEqualTo("Alice");
        assertThat(order.status()).isEqualTo("Created");
        assertThat(order.items()).hasSize(1);

        // Downstream kitchen received the preparation request (tracked HTTP call).
        assertThat(BreakfastBackends.kitchen().receivedPreparation()).isTrue();

        // The order is retrievable by id.
        TestResponse fetched = client.get("/orders/" + order.orderId());
        assertThat(fetched.status()).isEqualTo(200);
        assertThat(fetched.as(OrderResponse.class).orderId()).isEqualTo(order.orderId());

        // The outbox holds an OrderCreatedEvent that the processor eventually marks Processed.
        Awaitility.await().atMost(Duration.ofSeconds(15)).untilAsserted(() -> {
            List<OutboxMessage> messages = outboxStore.findAll();
            assertThat(messages).anyMatch(m -> "OrderCreatedEvent".equals(m.getEventType())
                    && "Processed".equals(m.getStatus()));
        });
    }

    @Test
    @DisplayName("retrieving a non-existent order returns 404")
    void getMissingOrder() {
        TestResponse response = client.get("/orders/" + UUID.randomUUID());
        assertThat(response.status()).isEqualTo(404);
    }

    @Test
    @DisplayName("a valid status transition updates the order")
    void validStatusTransition() {
        OrderResponse order = client.post("/orders", validOrder()).as(OrderResponse.class);

        TestResponse updated = client.patch("/orders/" + order.orderId() + "/status",
                new UpdateOrderStatusRequest("Preparing"));

        assertThat(updated.status()).isEqualTo(200);
        assertThat(updated.as(OrderResponse.class).status()).isEqualTo("Preparing");
    }

    @Test
    @DisplayName("an invalid status transition returns 409 Conflict")
    void invalidStatusTransition() {
        OrderResponse order = client.post("/orders", validOrder()).as(OrderResponse.class);

        TestResponse updated = client.patch("/orders/" + order.orderId() + "/status",
                new UpdateOrderStatusRequest("Ready"));

        assertThat(updated.status()).isEqualTo(409);
    }

    @Test
    @DisplayName("an order without a customer name is rejected with 400")
    void validationRejectsMissingCustomer() {
        OrderRequest invalid = new OrderRequest(null, List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 1)), 1);

        TestResponse response = client.post("/orders", invalid);

        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Customer Name' is required.")).isTrue();
    }

    @Test
    @DisplayName("an order moves through its complete lifecycle to Completed")
    void completeLifecycle() {
        String id = client.post("/orders", validOrder()).as(OrderResponse.class).orderId().toString();

        assertThat(client.patch("/orders/" + id + "/status", new UpdateOrderStatusRequest("Preparing")).status())
                .isEqualTo(200);
        assertThat(client.patch("/orders/" + id + "/status", new UpdateOrderStatusRequest("Ready")).status())
                .isEqualTo(200);
        TestResponse completed = client.patch("/orders/" + id + "/status", new UpdateOrderStatusRequest("Completed"));

        assertThat(completed.status()).isEqualTo(200);
        assertThat(completed.as(OrderResponse.class).status()).isEqualTo("Completed");
    }

    @Test
    @DisplayName("an order is still created when the kitchen service fails")
    void kitchenFailureStillCreatesOrder() {
        BreakfastBackends.kitchen().setNextStatus(503);

        TestResponse response = client.post("/orders", validOrder());

        assertThat(response.status()).isEqualTo(201);
        assertThat(response.as(OrderResponse.class).customerName()).isEqualTo("Alice");
    }

    @Test
    @DisplayName("orders are returned with pagination metadata")
    void pagination() {
        String customer = "Page-" + UUID.randomUUID();
        client.post("/orders", new OrderRequest(customer, List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 1)), 1));
        client.post("/orders", new OrderRequest(customer, List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 1)), 2));

        TestResponse page = client.get("/orders?page=1&pageSize=1");

        assertThat(page.status()).isEqualTo(200);
        com.fasterxml.jackson.databind.JsonNode body = page.json();
        assertThat(body.get("page").asInt()).isEqualTo(1);
        assertThat(body.get("pageSize").asInt()).isEqualTo(1);
        assertThat(body.get("items").size()).isEqualTo(1);
        assertThat(body.get("totalCount").asInt()).isGreaterThanOrEqualTo(2);
    }
}
