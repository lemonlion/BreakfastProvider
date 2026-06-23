package io.lemonlion.breakfast.tests.spock

import io.lemonlion.breakfast.BreakfastProviderApplication
import io.lemonlion.breakfast.model.event.RecipeCostCalculatedEvent
import io.lemonlion.breakfast.reporting.RecipeCostConsumer
import io.lemonlion.breakfast.testsupport.BackendsInitializer
import io.lemonlion.breakfast.testsupport.BreakfastBackends
import io.lemonlion.breakfast.testsupport.BreakfastTestClient
import io.lemonlion.breakfast.testsupport.JsonMappers
import org.awaitility.Awaitility
import org.springframework.beans.factory.annotation.Autowired
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.boot.test.web.server.LocalServerPort
import org.springframework.kafka.core.KafkaTemplate
import org.springframework.test.context.ContextConfiguration
import spock.lang.Specification

import java.time.Duration
import java.time.Instant

/** RecipeCosts domain component spec (Spock) — Kafka consumer. */
@SpringBootTest(classes = BreakfastProviderApplication, webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer)
class RecipeCostsSpec extends Specification {

    @LocalServerPort
    int port

    @Autowired
    KafkaTemplate<String, String> kafkaTemplate

    BreakfastTestClient client

    def setup() {
        client = new BreakfastTestClient("http://127.0.0.1:${port}")
        BreakfastBackends.resetFakes()
    }

    def "consuming a recipe-cost event notifies the kitchen"() {
        when:
        def event = new RecipeCostCalculatedEvent(UUID.randomUUID(), "Classic Pancakes", ["Flour", "Milk"],
                new BigDecimal("3.50"), "GBP", Instant.now())
        kafkaTemplate.send(RecipeCostConsumer.TOPIC, JsonMappers.instance().writeValueAsString(event))

        then:
        Awaitility.await().atMost(Duration.ofSeconds(30)).untilAsserted {
            assert BreakfastBackends.kitchen().receivedPreparation()
        }
    }
}
