package io.lemonlion.breakfast.tests.testng;

import static org.assertj.core.api.Assertions.assertThat;

import io.lemonlion.breakfast.model.response.GoatMilkResponse;
import io.lemonlion.breakfast.model.response.MilkResponse;
import io.lemonlion.breakfast.testsupport.BreakfastBackends;
import io.lemonlion.breakfast.testsupport.TestResponse;
import org.testng.annotations.Test;

/** Milk-sourcing domain component tests (TestNG). */
public class MilkSourcingTestNgTest extends ComponentTestBaseNg {

    @Test
    public void sourcesCowMilk() {
        TestResponse response = client.get("/milk");
        assertThat(response.status()).isEqualTo(200);
        assertThat(response.as(MilkResponse.class).milk()).isEqualTo("Some_Milk");
    }

    @Test
    public void sourcesGoatMilk() {
        TestResponse response = client.get("/goat-milk");
        assertThat(response.status()).isEqualTo(200);
        assertThat(response.as(GoatMilkResponse.class).goatMilk()).isEqualTo("Some_Fresh_Goat_Milk");
    }

    @Test
    public void cowFailureReturns502() {
        BreakfastBackends.cow().setStatus(503);
        assertThat(client.get("/milk").status()).isEqualTo(502);
    }
}
