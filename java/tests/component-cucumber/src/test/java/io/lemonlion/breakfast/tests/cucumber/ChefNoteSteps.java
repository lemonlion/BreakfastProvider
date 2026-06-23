package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.request.ChefNoteRequest;
import io.lemonlion.breakfast.model.response.ChefNoteResponse;

/** Cucumber step definitions for the ChefNotes domain. */
public class ChefNoteSteps {

    private final ScenarioContext ctx;

    public ChefNoteSteps(ScenarioContext ctx) {
        this.ctx = ctx;
    }

    @When("a chef note for recipe {string} is recorded")
    public void aChefNoteIsRecorded(String recipe) {
        ctx.lastResponse = ctx.client().post("/chef-notes",
                new ChefNoteRequest(recipe, "Chef Remy", "Rest the batter.", "Technique"));
    }

    @Then("the chef note is stored with an id")
    public void theChefNoteIsStored() {
        assertThat(ctx.lastResponse.status()).isEqualTo(201);
        assertThat(ctx.lastResponse.as(ChefNoteResponse.class).noteId()).isNotBlank();
    }
}
