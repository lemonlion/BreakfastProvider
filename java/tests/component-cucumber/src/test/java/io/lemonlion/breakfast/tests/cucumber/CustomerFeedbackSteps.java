package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.event.CustomerFeedbackReceivedEvent;
import io.lemonlion.breakfast.testsupport.BreakfastBackends;
import java.time.Duration;
import java.time.Instant;
import java.util.UUID;
import org.awaitility.Awaitility;

/** Cucumber step definitions for the CustomerFeedback (Pub/Sub consumer) domain. */
public class CustomerFeedbackSteps {

    private final ScenarioContext ctx;

    public CustomerFeedbackSteps(ScenarioContext ctx) {
        this.ctx = ctx;
    }

    @When("a customer feedback event is published")
    public void aCustomerFeedbackEventIsPublished() {
        BreakfastBackends.publishCustomerFeedback(new CustomerFeedbackReceivedEvent(
                UUID.randomUUID(), "Alice", "Classic Pancakes", 5, "Loved it", Instant.now()));
    }

    @Then("the supplier service is notified of the feedback")
    public void theSupplierServiceIsNotified() {
        Awaitility.await().atMost(Duration.ofSeconds(30)).untilAsserted(() ->
                assertThat(BreakfastBackends.supplier().receivedFeedback()).isTrue());
    }
}
