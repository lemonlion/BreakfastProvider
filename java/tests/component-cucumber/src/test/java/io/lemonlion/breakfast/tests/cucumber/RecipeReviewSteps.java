package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.request.RecipeReviewRequest;
import io.lemonlion.breakfast.model.response.RecipeReviewResponse;
import java.util.List;

/** Cucumber step definitions for the RecipeReviews domain. */
public class RecipeReviewSteps {

    private final ScenarioContext ctx;

    public RecipeReviewSteps(ScenarioContext ctx) {
        this.ctx = ctx;
    }

    @When("a review for {string} with rating {int} is submitted")
    public void aReviewIsSubmitted(String recipe, int rating) {
        ctx.lastResponse = ctx.client().post("/recipe-reviews",
                new RecipeReviewRequest(recipe, "Alice", rating, "Tasty", List.of("fluffy")));
    }

    @Then("the review is stored with rating {int}")
    public void theReviewIsStored(int rating) {
        assertThat(ctx.lastResponse.status()).isEqualTo(201);
        assertThat(ctx.lastResponse.as(RecipeReviewResponse.class).rating()).isEqualTo(rating);
    }
}
