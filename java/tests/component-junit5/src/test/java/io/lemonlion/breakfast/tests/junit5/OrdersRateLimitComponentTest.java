package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;

import io.lemonlion.breakfast.model.request.OrderItemRequest;
import io.lemonlion.breakfast.model.request.OrderRequest;
import java.util.List;
import java.util.UUID;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.test.context.TestPropertySource;

/**
 * Orders rate-limiting (JUnit 5). Runs in its own Spring context with the permit limit overridden to 1
 * (twin of the C# Rate_Limiting scenario, which recreates the app with PermitLimit=1) so the second
 * order within the window is rejected with 429.
 */
@DisplayName("Orders rate limiting")
@TestPropertySource(properties = {
        "rate-limit.permit-limit=1",
        "rate-limit.window-seconds=60",
        // Unique in-process gRPC name so this extra context doesn't collide with the shared one.
        "grpc.server.in-process-name=breakfast-grpc-ratelimit"})
class OrdersRateLimitComponentTest extends ComponentTestBase {

    @Test
    @DisplayName("a second order within the window is rate limited with 429")
    void secondOrderIsRateLimited() {
        OrderRequest order = new OrderRequest(
                "RateLimit-" + UUID.randomUUID(),
                List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 1)), 1);

        assertThat(client.post("/orders", order).status()).isEqualTo(201);
        assertThat(client.post("/orders", order).status()).isEqualTo(429);
    }
}
