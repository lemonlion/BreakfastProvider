package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.request.StaffMemberRequest;
import io.lemonlion.breakfast.model.response.StaffMemberResponse;

/** Cucumber step definitions for the Staff domain. */
public class StaffSteps {

    private final ScenarioContext ctx;

    public StaffSteps(ScenarioContext ctx) {
        this.ctx = ctx;
    }

    @When("a staff member {string} with role {string} is added")
    public void aStaffMemberIsAdded(String name, String role) {
        ctx.lastResponse = ctx.client().post("/staff",
                new StaffMemberRequest(name, role, "staff@example.com", true, null));
    }

    @Then("the staff member is created with a role of {string}")
    public void theStaffMemberIsCreated(String role) {
        assertThat(ctx.lastResponse.status()).isEqualTo(201);
        assertThat(ctx.lastResponse.as(StaffMemberResponse.class).role()).isEqualTo(role);
    }
}
