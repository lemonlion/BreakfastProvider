package io.lemonlion.breakfast.tests.testng;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.core.type.TypeReference;
import io.lemonlion.breakfast.model.request.OrderItemRequest;
import io.lemonlion.breakfast.model.request.OrderRequest;
import io.lemonlion.breakfast.model.response.ToppingResponse;
import java.util.List;
import java.util.UUID;
import org.springframework.test.context.TestPropertySource;
import org.testng.annotations.Test;

/**
 * Configuration-override scenarios (TestNG), consolidated into a single extra Spring context to keep the
 * number of heavyweight backend-bearing contexts small. Covers the C# Rate_Limiting, Toppings Feature_Flag
 * (disabled) and Ingredients Goat_Milk_Feature_Flag (disabled) scenarios; only the rate-limit test creates
 * orders, so the overrides don't interfere.
 */
@TestPropertySource(properties = {
        "rate-limit.permit-limit=1",
        "rate-limit.window-seconds=60",
        "feature-switches.raspberry-topping-enabled=false",
        "feature-switches.goat-milk-enabled=false",
        "grpc.server.in-process-name=breakfast-grpc-overrides-ng"})
public class OverridesTestNgTest extends ComponentTestBaseNg {

    @Test
    public void secondOrderIsRateLimited() {
        OrderRequest order = new OrderRequest(
                "RateLimit-" + UUID.randomUUID(),
                List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 1)), 1);

        assertThat(client.post("/orders", order).status()).isEqualTo(201);
        assertThat(client.post("/orders", order).status()).isEqualTo(429);
    }

    @Test
    public void raspberriesExcludedWhenDisabled() {
        List<ToppingResponse> toppings = client.get("/toppings")
                .as(new TypeReference<List<ToppingResponse>>() { });
        assertThat(toppings).extracting(ToppingResponse::name).doesNotContain("Raspberries");
    }

    @Test
    public void goatMilkDisabledReturns404() {
        assertThat(client.get("/goat-milk").status()).isEqualTo(404);
    }
}
