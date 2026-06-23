package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;

import io.lemonlion.breakfast.model.response.GoatMilkResponse;
import io.lemonlion.breakfast.model.response.MilkResponse;
import io.lemonlion.breakfast.testsupport.BreakfastBackends;
import io.lemonlion.breakfast.testsupport.TestResponse;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

/** Milk-sourcing domain component tests (JUnit 5) — Cow/Goat HTTP downstream. */
@DisplayName("MilkSourcing")
class MilkSourcingComponentTest extends ComponentTestBase {

    @Test
    @DisplayName("milk is sourced from the cow service")
    void sourcesCowMilk() {
        TestResponse response = client.get("/milk");
        assertThat(response.status()).isEqualTo(200);
        assertThat(response.as(MilkResponse.class).milk()).isEqualTo("Some_Milk");
    }

    @Test
    @DisplayName("goat milk is sourced when the feature is enabled")
    void sourcesGoatMilk() {
        TestResponse response = client.get("/goat-milk");
        assertThat(response.status()).isEqualTo(200);
        assertThat(response.as(GoatMilkResponse.class).goatMilk()).isEqualTo("Some_Fresh_Goat_Milk");
    }

    @Test
    @DisplayName("a cow service failure returns 502 Bad Gateway")
    void cowFailureReturns502() {
        BreakfastBackends.cow().setStatus(503);
        TestResponse response = client.get("/milk");
        assertThat(response.status()).isEqualTo(502);
    }
}
