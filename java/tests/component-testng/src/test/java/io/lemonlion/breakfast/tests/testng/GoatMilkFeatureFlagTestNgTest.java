package io.lemonlion.breakfast.tests.testng;

import static org.assertj.core.api.Assertions.assertThat;

import org.springframework.test.context.TestPropertySource;
import org.testng.annotations.Test;

/** Goat-milk feature-flag scenario (TestNG): flag disabled -> GET /goat-milk returns 404. */
@TestPropertySource(properties = {
        "feature-switches.goat-milk-enabled=false",
        "grpc.server.in-process-name=breakfast-grpc-goatflag-ng"})
public class GoatMilkFeatureFlagTestNgTest extends ComponentTestBaseNg {

    @Test
    public void goatMilkDisabledReturns404() {
        assertThat(client.get("/goat-milk").status()).isEqualTo(404);
    }
}
