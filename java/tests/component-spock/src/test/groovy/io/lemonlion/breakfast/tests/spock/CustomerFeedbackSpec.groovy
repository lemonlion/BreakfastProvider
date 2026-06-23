package io.lemonlion.breakfast.tests.spock

import io.lemonlion.breakfast.BreakfastProviderApplication
import io.lemonlion.breakfast.model.event.CustomerFeedbackReceivedEvent
import io.lemonlion.breakfast.testsupport.BackendsInitializer
import io.lemonlion.breakfast.testsupport.BreakfastBackends
import io.lemonlion.breakfast.testsupport.BreakfastTestClient
import org.awaitility.Awaitility
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.boot.test.web.server.LocalServerPort
import org.springframework.test.context.ContextConfiguration
import spock.lang.Specification

import java.time.Duration
import java.time.Instant

/** CustomerFeedback domain component spec (Spock) — Pub/Sub consumer. */
@SpringBootTest(classes = BreakfastProviderApplication, webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer)
class CustomerFeedbackSpec extends Specification {

    @LocalServerPort
    int port

    BreakfastTestClient client

    def setup() {
        client = new BreakfastTestClient("http://127.0.0.1:${port}")
        BreakfastBackends.resetFakes()
    }

    def "consuming a feedback event triggers downstream supplier processing"() {
        when:
        BreakfastBackends.publishCustomerFeedback(
                new CustomerFeedbackReceivedEvent(UUID.randomUUID(), "Alice", "Classic Pancakes", 5, "Loved it", Instant.now()))

        then:
        Awaitility.await().atMost(Duration.ofSeconds(30)).untilAsserted {
            assert BreakfastBackends.supplier().receivedFeedback()
        }
    }
}
