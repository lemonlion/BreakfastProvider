package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.request.ChefNoteRequest;
import io.lemonlion.breakfast.model.request.UpdateChefNoteRequest;
import io.lemonlion.breakfast.model.response.ChefNoteResponse;

/** Cucumber step definitions for the ChefNotes domain. */
public class ChefNoteSteps {

    private final ScenarioContext ctx;
    private String recordedNoteId;

    public ChefNoteSteps(ScenarioContext ctx) {
        this.ctx = ctx;
    }

    @When("a chef note for recipe {string} is recorded")
    public void aChefNoteIsRecorded(String recipe) {
        ctx.lastResponse = ctx.client().post("/chef-notes",
                new ChefNoteRequest(recipe, "Chef Remy", "Rest the batter.", "Technique"));
        if (ctx.lastResponse.status() == 201) {
            recordedNoteId = ctx.lastResponse.as(ChefNoteResponse.class).noteId();
        }
    }

    @When("a chef note without note text is recorded")
    public void aChefNoteWithoutNoteTextIsRecorded() {
        ctx.lastResponse = ctx.client().post("/chef-notes",
                new ChefNoteRequest("Classic Pancakes", "Chef Remy", "", "Technique"));
    }

    @When("the recorded chef note is retrieved")
    public void theRecordedChefNoteIsRetrieved() {
        ctx.lastResponse = ctx.client().get("/chef-notes/" + recordedNoteId);
    }

    @When("a non-existent chef note is retrieved")
    public void aNonExistentChefNoteIsRetrieved() {
        ctx.lastResponse = ctx.client().get("/chef-notes/does-not-exist");
    }

    @When("the recorded chef note is updated")
    public void theRecordedChefNoteIsUpdated() {
        ctx.lastResponse = ctx.client().patch("/chef-notes/" + recordedNoteId,
                new UpdateChefNoteRequest("Rest the batter for 20 minutes.", "Technique"));
    }

    @When("a non-existent chef note is updated")
    public void aNonExistentChefNoteIsUpdated() {
        ctx.lastResponse = ctx.client().patch("/chef-notes/does-not-exist",
                new UpdateChefNoteRequest("Updated", "Technique"));
    }

    @When("chef notes for recipe {string} are listed")
    public void chefNotesForRecipeAreListed(String recipe) {
        ctx.lastResponse = ctx.client().get("/chef-notes/recipe/" + recipe);
    }

    @Then("the chef note is stored with an id")
    public void theChefNoteIsStored() {
        assertThat(ctx.lastResponse.status()).isEqualTo(201);
        assertThat(ctx.lastResponse.as(ChefNoteResponse.class).noteId()).isNotBlank();
    }

    @Then("the retrieved chef note has chef {string}")
    public void theRetrievedChefNoteHasChef(String chef) {
        assertThat(ctx.lastResponse.status()).isEqualTo(200);
        assertThat(ctx.lastResponse.as(ChefNoteResponse.class).chefName()).isEqualTo(chef);
    }

    @Then("the listed chef notes include the recorded note")
    public void theListedChefNotesIncludeTheRecordedNote() {
        assertThat(ctx.lastResponse.status()).isEqualTo(200);
        assertThat(ctx.lastResponse.bodyContains("Rest the batter.")).isTrue();
    }
}
