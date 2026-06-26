package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.core.type.TypeReference;
import io.lemonlion.breakfast.model.response.ToppingResponse;
import java.util.List;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.test.context.TestPropertySource;

/**
 * Toppings feature-flag scenario (JUnit 5): with the raspberry topping flag disabled (own Spring context),
 * the catalogue excludes Raspberries — twin of the C# Toppings Feature_Flag scenario.
 */
@DisplayName("Toppings feature flag")
@TestPropertySource(properties = {
        "feature-switches.raspberry-topping-enabled=false",
        "grpc.server.in-process-name=breakfast-grpc-toppingsflag"})
class ToppingsFeatureFlagComponentTest extends ComponentTestBase {

    @Test
    @DisplayName("raspberries are excluded when the feature flag is disabled")
    void raspberriesExcludedWhenDisabled() {
        List<ToppingResponse> toppings = client.get("/toppings")
                .as(new TypeReference<List<ToppingResponse>>() { });

        assertThat(toppings).extracting(ToppingResponse::name).doesNotContain("Raspberries");
    }
}
