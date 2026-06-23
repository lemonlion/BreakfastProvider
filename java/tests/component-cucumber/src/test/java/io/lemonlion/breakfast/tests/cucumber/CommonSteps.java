package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import io.cucumber.java.en.Then;

/** Shared Then-steps usable by every domain feature, backed by {@link ScenarioContext}. */
public class CommonSteps {

    private final ScenarioContext ctx;

    public CommonSteps(ScenarioContext ctx) {
        this.ctx = ctx;
    }

    @Then("the response status is {int}")
    public void theResponseStatusIs(int code) {
        assertThat(ctx.lastResponse.status()).isEqualTo(code);
    }

    @Then("the error mentions {string}")
    public void theErrorMentions(String message) {
        assertThat(ctx.lastResponse.bodyContains(message)).isTrue();
    }
}
