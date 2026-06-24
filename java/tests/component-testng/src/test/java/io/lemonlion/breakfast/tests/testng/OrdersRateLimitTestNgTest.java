package io.lemonlion.breakfast.tests.testng;

import static org.assertj.core.api.Assertions.assertThat;

import io.lemonlion.breakfast.model.request.OrderItemRequest;
import io.lemonlion.breakfast.model.request.OrderRequest;
import java.util.List;
import java.util.UUID;
import org.springframework.test.context.TestPropertySource;
import org.testng.annotations.Test;

/**
 * Orders rate-limiting (TestNG). Own Spring context with the permit limit overridden to 1 (twin of the
 * C# Rate_Limiting scenario) so the second order within the window is rejected with 429. A unique
 * in-process gRPC name keeps this extra context from colliding with the shared one.
 */
@TestPropertySource(properties = {
        "rate-limit.permit-limit=1",
        "rate-limit.window-seconds=60",
        "grpc.server.in-process-name=breakfast-grpc-ratelimit-ng"})
public class OrdersRateLimitTestNgTest extends ComponentTestBaseNg {

    @Test
    public void secondOrderIsRateLimited() {
        OrderRequest order = new OrderRequest(
                "RateLimit-" + UUID.randomUUID(),
                List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 1)), 1);

        assertThat(client.post("/orders", order).status()).isEqualTo(201);
        assertThat(client.post("/orders", order).status()).isEqualTo(429);
    }
}
