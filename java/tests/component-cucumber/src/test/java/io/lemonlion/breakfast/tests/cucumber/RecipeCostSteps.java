package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.event.RecipeCostCalculatedEvent;
import io.lemonlion.breakfast.reporting.RecipeCostConsumer;
import io.lemonlion.breakfast.testsupport.BreakfastBackends;
import io.lemonlion.breakfast.testsupport.JsonMappers;
import java.math.BigDecimal;
import java.time.Duration;
import java.time.Instant;
import java.util.List;
import java.util.UUID;
import org.awaitility.Awaitility;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.kafka.core.KafkaTemplate;

/** Cucumber step definitions for the RecipeCosts (Kafka consumer) domain. */
public class RecipeCostSteps {

    private final ScenarioContext ctx;

    @Autowired
    KafkaTemplate<String, String> kafkaTemplate;

    public RecipeCostSteps(ScenarioContext ctx) {
        this.ctx = ctx;
    }

    @When("a recipe-cost calculated event is published")
    public void aRecipeCostEventIsPublished() throws Exception {
        RecipeCostCalculatedEvent event = new RecipeCostCalculatedEvent(
                UUID.randomUUID(), "Classic Pancakes", List.of("Flour", "Milk"),
                new BigDecimal("3.50"), "GBP", Instant.now());
        kafkaTemplate.send(RecipeCostConsumer.TOPIC, JsonMappers.instance().writeValueAsString(event));
    }

    @Then("the kitchen is notified of the recipe cost")
    public void theKitchenIsNotified() {
        Awaitility.await().atMost(Duration.ofSeconds(30)).untilAsserted(() ->
                assertThat(BreakfastBackends.kitchen().receivedPreparation()).isTrue());
    }
}
