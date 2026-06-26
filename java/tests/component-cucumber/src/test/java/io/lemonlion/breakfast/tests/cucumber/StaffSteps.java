package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.request.StaffMemberRequest;
import io.lemonlion.breakfast.model.response.StaffMemberResponse;

/** Cucumber step definitions for the Staff domain. */
public class StaffSteps {

    private final ScenarioContext ctx;
    private long createdId;

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

    @When("a staff member is added and retrieved by id")
    public void aStaffMemberIsAddedAndRetrievedById() {
        createdId = ctx.client().post("/staff",
                        new StaffMemberRequest("Sam Cook", "Chef", "sam@example.com", true, null))
                .as(StaffMemberResponse.class).id();
        ctx.lastResponse = ctx.client().get("/staff/" + createdId);
    }

    @Then("the retrieved staff member has id matching the created one")
    public void theRetrievedStaffMemberMatches() {
        assertThat(ctx.lastResponse.status()).isEqualTo(200);
        assertThat(ctx.lastResponse.as(StaffMemberResponse.class).id()).isEqualTo(createdId);
    }

    @When("a staff member is added and deleted")
    public void aStaffMemberIsAddedAndDeleted() {
        createdId = ctx.client().post("/staff",
                        new StaffMemberRequest("Sam Cook", "Chef", "sam@example.com", true, null))
                .as(StaffMemberResponse.class).id();
        ctx.lastResponse = ctx.client().delete("/staff/" + createdId);
    }
}
