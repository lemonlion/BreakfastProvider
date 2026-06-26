package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.core.type.TypeReference;
import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.request.FeedbackRequest;
import io.lemonlion.breakfast.model.response.FeedbackResponse;
import java.util.List;
import java.util.UUID;

/** Cucumber step definitions for the Feedback domain. */
public class FeedbackSteps {

    private final ScenarioContext ctx;
    private String createdFeedbackId;
    private String orderId;

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

    @When("valid feedback is submitted and retrieved by id")
    public void validFeedbackIsSubmittedAndRetrievedById() {
        FeedbackResponse created = ctx.client().post("/feedback",
                        new FeedbackRequest("Alice", "order-" + UUID.randomUUID(), 4, "Great pancakes!"))
                .as(FeedbackResponse.class);
        createdFeedbackId = created.feedbackId();
        ctx.lastResponse = ctx.client().get("/feedback/" + createdFeedbackId);
    }

    @Then("the retrieved feedback matches the submitted feedback")
    public void theRetrievedFeedbackMatches() {
        assertThat(ctx.lastResponse.status()).isEqualTo(200);
        FeedbackResponse fetched = ctx.lastResponse.as(FeedbackResponse.class);
        assertThat(fetched.feedbackId()).isEqualTo(createdFeedbackId);
        assertThat(fetched.rating()).isEqualTo(4);
    }

    @When("valid feedback is submitted and listed for its order")
    public void validFeedbackIsSubmittedAndListedForItsOrder() {
        orderId = "order-" + UUID.randomUUID();
        FeedbackResponse created = ctx.client().post("/feedback",
                new FeedbackRequest("Bob", orderId, 3, "Decent")).as(FeedbackResponse.class);
        createdFeedbackId = created.feedbackId();
        ctx.lastResponse = ctx.client().get("/feedback/order/" + orderId);
    }

    @Then("the order feedback list contains the submitted feedback")
    public void theOrderFeedbackListContainsTheSubmittedFeedback() {
        assertThat(ctx.lastResponse.status()).isEqualTo(200);
        assertThat(ctx.lastResponse.as(new TypeReference<List<FeedbackResponse>>() { }))
                .anyMatch(f -> f.feedbackId().equals(createdFeedbackId));
    }

    @When("a non-existent feedback is retrieved")
    public void aNonExistentFeedbackIsRetrieved() {
        ctx.lastResponse = ctx.client().get("/feedback/unknown-" + UUID.randomUUID());
    }

    @When("feedback with a missing customer name is submitted")
    public void feedbackWithMissingCustomerNameIsSubmitted() {
        ctx.lastResponse = ctx.client().post("/feedback",
                new FeedbackRequest(null, "order-" + UUID.randomUUID(), 3, "Missing name"));
    }
}
