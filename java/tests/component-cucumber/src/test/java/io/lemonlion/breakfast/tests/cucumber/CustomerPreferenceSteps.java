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

    @When("a preference is saved and retrieved by id")
    public void aPreferenceIsSavedAndRetrievedById() {
        customerId = "cust-" + UUID.randomUUID();
        ctx.client().put("/customer-preferences/" + customerId,
                new CustomerPreferenceRequest(null, "Alice", "oat", true, "Pancakes"));
        ctx.lastResponse = ctx.client().get("/customer-preferences/" + customerId);
    }

    @Then("the retrieved preference is for {string}")
    public void theRetrievedPreferenceIsFor(String customerName) {
        assertThat(ctx.lastResponse.status()).isEqualTo(200);
        assertThat(ctx.lastResponse.as(CustomerPreferenceResponse.class).customerName()).isEqualTo(customerName);
    }

    @When("a saved preference is updated to {string} milk")
    public void aSavedPreferenceIsUpdatedTo(String milk) {
        customerId = "cust-" + UUID.randomUUID();
        ctx.client().put("/customer-preferences/" + customerId,
                new CustomerPreferenceRequest(null, "Alice", "oat", true, "Pancakes"));
        ctx.lastResponse = ctx.client().put("/customer-preferences/" + customerId,
                new CustomerPreferenceRequest(null, "Alice", milk, false, "Waffles"));
    }

    @When("a non-existent customer preference is retrieved")
    public void aNonExistentCustomerPreferenceIsRetrieved() {
        ctx.lastResponse = ctx.client().get("/customer-preferences/unknown-" + UUID.randomUUID());
    }

    @When("a preference with a missing customer name is saved")
    public void aPreferenceWithMissingCustomerNameIsSaved() {
        ctx.lastResponse = ctx.client().put("/customer-preferences/cust-x",
                new CustomerPreferenceRequest(null, "", "oat", false, ""));
    }
}
