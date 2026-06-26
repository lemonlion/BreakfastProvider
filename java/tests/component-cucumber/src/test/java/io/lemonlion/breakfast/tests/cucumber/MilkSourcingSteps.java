package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import io.cucumber.java.en.Given;
import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.response.GoatMilkResponse;
import io.lemonlion.breakfast.model.response.MilkResponse;
import io.lemonlion.breakfast.testsupport.BreakfastBackends;

/** Cucumber step definitions for the milk-sourcing domain. */
public class MilkSourcingSteps {

    private final ScenarioContext ctx;

    public MilkSourcingSteps(ScenarioContext ctx) {
        this.ctx = ctx;
    }

    @Given("the cow service is unavailable")
    public void theCowServiceIsUnavailable() {
        BreakfastBackends.cow().setStatus(503);
    }

    @Given("the cow service returns an invalid response")
    public void theCowServiceReturnsInvalid() {
        BreakfastBackends.cow().setInvalidResponse(true);
    }

    @Given("the goat service is unavailable")
    public void theGoatServiceIsUnavailable() {
        BreakfastBackends.goat().setStatus(503);
    }

    @Given("the goat service returns an invalid response")
    public void theGoatServiceReturnsInvalid() {
        BreakfastBackends.goat().setInvalidResponse(true);
    }

    @When("milk is sourced")
    public void milkIsSourced() {
        ctx.lastResponse = ctx.client().get("/milk");
    }

    @When("goat milk is sourced")
    public void goatMilkIsSourced() {
        ctx.lastResponse = ctx.client().get("/goat-milk");
    }

    @Then("fresh milk is returned")
    public void freshMilkIsReturned() {
        assertThat(ctx.lastResponse.status()).isEqualTo(200);
        assertThat(ctx.lastResponse.as(MilkResponse.class).milk()).isEqualTo("Some_Milk");
    }

    @Then("fresh goat milk is returned")
    public void freshGoatMilkIsReturned() {
        assertThat(ctx.lastResponse.status()).isEqualTo(200);
        assertThat(ctx.lastResponse.as(GoatMilkResponse.class).goatMilk()).isEqualTo("Some_Fresh_Goat_Milk");
    }
}
