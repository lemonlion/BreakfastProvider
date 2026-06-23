package io.lemonlion.breakfast.tests.testng;

import static org.assertj.core.api.Assertions.assertThat;

import io.lemonlion.breakfast.model.event.CustomerFeedbackReceivedEvent;
import io.lemonlion.breakfast.testsupport.BreakfastBackends;
import java.time.Duration;
import java.time.Instant;
import java.util.UUID;
import org.awaitility.Awaitility;
import org.testng.annotations.Test;

/** CustomerFeedback domain component tests (TestNG) — Pub/Sub consumer. */
public class CustomerFeedbackTestNgTest extends ComponentTestBaseNg {

    @Test
    public void consumeFeedbackTriggersDownstream() {
        CustomerFeedbackReceivedEvent event = new CustomerFeedbackReceivedEvent(
                UUID.randomUUID(), "Alice", "Classic Pancakes", 5, "Loved it", Instant.now());

        BreakfastBackends.publishCustomerFeedback(event);

        Awaitility.await().atMost(Duration.ofSeconds(30)).untilAsserted(() ->
                assertThat(BreakfastBackends.supplier().receivedFeedback()).isTrue());
    }
}
