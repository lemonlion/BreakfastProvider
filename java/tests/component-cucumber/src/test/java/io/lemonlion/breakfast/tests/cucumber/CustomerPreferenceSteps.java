package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.request.CustomerPreferenceRequest;
import io.lemonlion.breakfast.model.response.CustomerPreferenceResponse;
import java.util.UUID;

/** Cucumber step definitions for the CustomerPreferences domain. */
public class CustomerPreferenceSteps {

    private final ScenarioContext ctx;
    private String customerId;

    public CustomerPreferenceSteps(ScenarioContext ctx) {
        this.ctx = ctx;
    }

    @When("a preference for {string} preferring {string} milk is saved")
    public void aPreferenceIsSaved(String customerName, String milk) {
        customerId = "cust-" + UUID.randomUUID();
        ctx.lastResponse = ctx.client().put("/customer-preferences/" + customerId,
                new CustomerPreferenceRequest(null, customerName, milk, true, "Pancakes"));
    }

    @Then("the saved preference uses {string} milk")
    public void theSavedPreferenceUses(String milk) {
        assertThat(ctx.lastResponse.status()).isEqualTo(200);
        assertThat(ctx.lastResponse.as(CustomerPreferenceResponse.class).preferredMilkType()).isEqualTo(milk);
    }
}
