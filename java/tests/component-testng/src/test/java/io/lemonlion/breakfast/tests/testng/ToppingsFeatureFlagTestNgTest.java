package io.lemonlion.breakfast.tests.testng;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.core.type.TypeReference;
import io.lemonlion.breakfast.model.response.ToppingResponse;
import java.util.List;
import org.springframework.test.context.TestPropertySource;
import org.testng.annotations.Test;

/** Toppings feature-flag scenario (TestNG): raspberry flag disabled -> catalogue excludes Raspberries. */
@TestPropertySource(properties = {
        "feature-switches.raspberry-topping-enabled=false",
        "grpc.server.in-process-name=breakfast-grpc-toppingsflag-ng"})
public class ToppingsFeatureFlagTestNgTest extends ComponentTestBaseNg {

    @Test
    public void raspberriesExcludedWhenDisabled() {
        List<ToppingResponse> toppings = client.get("/toppings")
                .as(new TypeReference<List<ToppingResponse>>() { });
        assertThat(toppings).extracting(ToppingResponse::name).doesNotContain("Raspberries");
    }
}
