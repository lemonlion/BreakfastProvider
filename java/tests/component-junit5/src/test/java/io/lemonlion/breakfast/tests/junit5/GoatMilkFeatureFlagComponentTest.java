package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;

import io.lemonlion.breakfast.testsupport.TestResponse;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.test.context.TestPropertySource;

/**
 * Goat-milk feature-flag scenario (JUnit 5): with the goat-milk flag disabled (own Spring context), the
 * endpoint returns 404 — twin of the C# Ingredients Goat_Milk_Feature_Flag (disabled) scenario.
 */
@DisplayName("Goat milk feature flag")
@TestPropertySource(properties = {
        "feature-switches.goat-milk-enabled=false",
        "grpc.server.in-process-name=breakfast-grpc-goatflag"})
class GoatMilkFeatureFlagComponentTest extends ComponentTestBase {

    @Test
    @DisplayName("the goat-milk endpoint returns 404 when the feature is disabled")
    void goatMilkDisabledReturns404() {
        TestResponse response = client.get("/goat-milk");

        assertThat(response.status()).isEqualTo(404);
    }
}
