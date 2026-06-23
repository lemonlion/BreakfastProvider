package io.lemonlion.breakfast.tests.testng;

import static org.assertj.core.api.Assertions.assertThat;

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
import org.testng.annotations.Test;

/** RecipeCosts domain component tests (TestNG). */
public class RecipeCostsTestNgTest extends ComponentTestBaseNg {

    @Autowired
    KafkaTemplate<String, String> kafkaTemplate;

    @Test
    public void consumeCostEventNotifiesKitchen() throws Exception {
        RecipeCostCalculatedEvent event = new RecipeCostCalculatedEvent(
                UUID.randomUUID(), "Classic Pancakes", List.of("Flour", "Milk"),
                new BigDecimal("3.50"), "GBP", Instant.now());

        kafkaTemplate.send(RecipeCostConsumer.TOPIC, JsonMappers.instance().writeValueAsString(event));

        Awaitility.await().atMost(Duration.ofSeconds(30)).untilAsserted(() ->
                assertThat(BreakfastBackends.kitchen().receivedPreparation()).isTrue());
    }
}
