package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;

import io.lemonlion.breakfast.model.event.CustomerFeedbackReceivedEvent;
import io.lemonlion.breakfast.testsupport.BreakfastBackends;
import java.time.Duration;
import java.time.Instant;
import java.util.UUID;
import org.awaitility.Awaitility;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

/** CustomerFeedback domain component tests (JUnit 5) — Pub/Sub consumer → Mongo + notify + supplier. */
@DisplayName("CustomerFeedback")
class CustomerFeedbackComponentTest extends ComponentTestBase {

    @Test
    @DisplayName("consuming a feedback event triggers downstream supplier processing")
    void consumeFeedbackTriggersDownstream() {
        CustomerFeedbackReceivedEvent event = new CustomerFeedbackReceivedEvent(
                UUID.randomUUID(), "Alice", "Classic Pancakes", 5, "Loved it", Instant.now());

        BreakfastBackends.publishCustomerFeedback(event);

        Awaitility.await().atMost(Duration.ofSeconds(30)).untilAsserted(() ->
                assertThat(BreakfastBackends.supplier().receivedFeedback()).isTrue());
    }
}
