package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.request.FeedbackRequest;
import io.lemonlion.breakfast.model.response.FeedbackResponse;
import java.util.UUID;

/** Cucumber step definitions for the Feedback domain. */
public class FeedbackSteps {

    private final ScenarioContext ctx;

    public FeedbackSteps(ScenarioContext ctx) {
        this.ctx = ctx;
    }

    @When("feedback with rating {int} is submitted")
    public void feedbackIsSubmitted(int rating) {
        ctx.lastResponse = ctx.client().post("/feedback",
                new FeedbackRequest("Alice", "order-" + UUID.randomUUID(), rating, "A comment"));
    }

    @Then("the feedback is stored with rating {int}")
    public void theFeedbackIsStored(int rating) {
        assertThat(ctx.lastResponse.status()).isEqualTo(201);
        assertThat(ctx.lastResponse.as(FeedbackResponse.class).rating()).isEqualTo(rating);
    }
}
