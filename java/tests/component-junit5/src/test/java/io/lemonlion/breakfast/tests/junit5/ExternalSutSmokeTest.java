package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;

import io.lemonlion.breakfast.grpc.RecipeSummaryReply;
import io.lemonlion.breakfast.grpc.RecipeSummaryRequest;
import io.lemonlion.breakfast.model.request.OrderItemRequest;
import io.lemonlion.breakfast.model.request.OrderRequest;
import io.lemonlion.breakfast.model.response.OrderResponse;
import io.lemonlion.breakfast.testsupport.GrpcSupport;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.List;
import java.util.UUID;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.condition.EnabledIfSystemProperty;

/**
 * External-sut smoke suite: a representative set of HTTP/gRPC behaviours driven against a deployed SUT
 * (no in-JVM fakes), proving the external-sut transport path end-to-end. Runs only when
 * {@code -Dexternal.sut.url} (and, for the gRPC case, {@code -Dexternal.grpc.target}) is set; disabled in
 * the default docker run. The full scenario suite can be brought to this mode incrementally — see
 * {@code docs/RUN_MODES.md}.
 *
 * <p>The {@code @EnabledIfSystemProperty} is repeated here (not just on the base) because JUnit 5 does not
 * inherit that condition from an abstract superclass.
 */
@DisplayName("External SUT smoke")
@EnabledIfSystemProperty(named = "external.sut.url", matches = ".+")
class ExternalSutSmokeTest extends ExternalSutComponentTestBase {

    @Test
    @DisplayName("the health endpoint responds")
    void healthResponds() {
        TestResponse response = client.get("/health");
        assertThat(response.status()).isEqualTo(200);
        assertThat(response.json().get("status").asText()).isNotBlank();
    }

    @Test
    @DisplayName("the menu endpoint returns items")
    void menuReturnsItems() {
        TestResponse response = client.get("/menu");
        assertThat(response.status()).isEqualTo(200);
    }

    @Test
    @DisplayName("an order is created and retrievable")
    void orderCreatedAndRetrievable() {
        OrderResponse order = client.post("/orders",
                        new OrderRequest("ExternalSut-" + UUID.randomUUID(),
                                List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 1)), 3))
                .as(OrderResponse.class);
        assertThat(order.orderId()).isNotNull();
        assertThat(client.get("/orders/" + order.orderId()).status()).isEqualTo(200);
    }

    @Test
    @DisplayName("the gRPC recipe summary responds")
    @EnabledIfSystemProperty(named = "external.grpc.target", matches = ".+")
    void grpcRecipeSummary() {
        RecipeSummaryReply reply = GrpcSupport.blockingStub()
                .getRecipeSummary(RecipeSummaryRequest.newBuilder().setRecipeType("Pancakes").build());
        assertThat(reply.getRecipeType()).isEqualTo("Pancakes");
        assertThat(reply.getTotalBatches()).isEqualTo(42);
    }
}
