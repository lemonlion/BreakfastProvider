package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.core.type.TypeReference;
import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.request.RecipeReviewRequest;
import io.lemonlion.breakfast.model.response.RecipeReviewResponse;
import java.util.List;
import java.util.UUID;

/** Cucumber step definitions for the RecipeReviews domain. */
public class RecipeReviewSteps {

    private final ScenarioContext ctx;
    private String createdReviewId;
    private String listedRecipe;

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

    @When("a review is submitted and retrieved by id")
    public void aReviewIsSubmittedAndRetrievedById() {
        createdReviewId = ctx.client().post("/recipe-reviews",
                        new RecipeReviewRequest("Classic Pancakes", "Alice", 5, "Delicious", List.of("fluffy")))
                .as(RecipeReviewResponse.class).reviewId();
        ctx.lastResponse = ctx.client().get("/recipe-reviews/" + createdReviewId);
    }

    @Then("the retrieved review matches the submitted review")
    public void theRetrievedReviewMatches() {
        assertThat(ctx.lastResponse.status()).isEqualTo(200);
        assertThat(ctx.lastResponse.as(RecipeReviewResponse.class).reviewId()).isEqualTo(createdReviewId);
    }

    @When("a review is submitted and listed by recipe")
    public void aReviewIsSubmittedAndListedByRecipe() {
        listedRecipe = "Recipe-" + UUID.randomUUID();
        createdReviewId = ctx.client().post("/recipe-reviews",
                        new RecipeReviewRequest(listedRecipe, "Alice", 5, "Delicious", List.of("fluffy")))
                .as(RecipeReviewResponse.class).reviewId();
        ctx.lastResponse = ctx.client().get("/recipe-reviews/recipe/" + listedRecipe);
    }

    @Then("the recipe review list contains the submitted review")
    public void theRecipeReviewListContainsTheSubmittedReview() {
        assertThat(ctx.lastResponse.status()).isEqualTo(200);
        assertThat(ctx.lastResponse.as(new TypeReference<List<RecipeReviewResponse>>() { }))
                .anyMatch(r -> r.reviewId().equals(createdReviewId));
    }

    @When("a non-existent review is retrieved")
    public void aNonExistentReviewIsRetrieved() {
        ctx.lastResponse = ctx.client().get("/recipe-reviews/unknown-" + UUID.randomUUID());
    }

    @When("a review with a missing recipe name is submitted")
    public void aReviewWithMissingRecipeNameIsSubmitted() {
        ctx.lastResponse = ctx.client().post("/recipe-reviews",
                new RecipeReviewRequest(null, "Alice", 5, "Delicious", List.of()));
    }
}
