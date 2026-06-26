package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.core.type.TypeReference;
import io.lemonlion.breakfast.model.request.OrderItemRequest;
import io.lemonlion.breakfast.model.request.OrderRequest;
import io.lemonlion.breakfast.model.response.ToppingResponse;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.List;
import java.util.UUID;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.test.context.TestPropertySource;

/**
 * Configuration-override scenarios (JUnit 5), all in a single extra Spring context so the suite keeps a
 * small number of heavyweight contexts (each context holds a full backend connection set). Covers the
 * C# Rate_Limiting, Toppings Feature_Flag (disabled) and Ingredients Goat_Milk_Feature_Flag (disabled)
 * scenarios; the overrides don't interfere (only the rate-limit test creates orders).
 */
@DisplayName("Configuration overrides")
@TestPropertySource(properties = {
        "rate-limit.permit-limit=1",
        "rate-limit.window-seconds=60",
        "feature-switches.raspberry-topping-enabled=false",
        "feature-switches.goat-milk-enabled=false",
        "grpc.server.in-process-name=breakfast-grpc-overrides"})
class OverridesComponentTest extends ComponentTestBase {

    @Test
    @DisplayName("a second order within the window is rate limited with 429")
    void secondOrderIsRateLimited() {
        OrderRequest order = new OrderRequest(
                "RateLimit-" + UUID.randomUUID(),
                List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 1)), 1);

        assertThat(client.post("/orders", order).status()).isEqualTo(201);
        assertThat(client.post("/orders", order).status()).isEqualTo(429);
    }

    @Test
    @DisplayName("raspberries are excluded when the feature flag is disabled")
    void raspberriesExcludedWhenDisabled() {
        List<ToppingResponse> toppings = client.get("/toppings")
                .as(new TypeReference<List<ToppingResponse>>() { });

        assertThat(toppings).extracting(ToppingResponse::name).doesNotContain("Raspberries");
    }

    @Test
    @DisplayName("the goat-milk endpoint returns 404 when the feature is disabled")
    void goatMilkDisabledReturns404() {
        TestResponse response = client.get("/goat-milk");

        assertThat(response.status()).isEqualTo(404);
    }
}
