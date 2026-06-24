package io.lemonlion.breakfast.tests.spock

import ch.qos.logback.classic.Logger
import ch.qos.logback.classic.spi.ILoggingEvent
import ch.qos.logback.core.read.ListAppender
import io.lemonlion.breakfast.BreakfastProviderApplication
import io.lemonlion.breakfast.model.request.OrderItemRequest
import io.lemonlion.breakfast.model.request.OrderRequest
import io.lemonlion.breakfast.testsupport.BackendsInitializer
import io.lemonlion.breakfast.testsupport.BreakfastBackends
import io.lemonlion.breakfast.testsupport.BreakfastTestClient
import org.slf4j.LoggerFactory
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.boot.test.web.server.LocalServerPort
import org.springframework.test.context.ContextConfiguration
import spock.lang.Specification

/** Infrastructure component spec (Spock): heartbeat, health checks, correlation id. */
@SpringBootTest(classes = BreakfastProviderApplication, webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer)
class InfrastructureSpec extends Specification {

    static final List<String> CHECKS =
            ["CowService", "GoatService", "SupplierService", "KitchenService", "CosmosDb", "Kafka"]
    static final List<String> DOWNSTREAM =
            ["CowService", "GoatService", "SupplierService", "KitchenService"]

    @LocalServerPort
    int port

    BreakfastTestClient client

    def setup() {
        client = new BreakfastTestClient("http://127.0.0.1:${port}")
        BreakfastBackends.resetFakes()
    }

    def "the heartbeat endpoint reports the service is running"() {
        when:
        def response = client.get("/")

        then:
        response.status() == 200
        response.json().get("status").asText() == "ok"
    }

    def "the health check reports healthy with all dependencies"() {
        when:
        def response = client.get("/health")

        then:
        response.status() == 200
        def body = response.json()
        body.get("status").asText() == "Healthy"
        CHECKS.every { body.get("results").has(it) }
    }

    def "the health check response contains detailed entries"() {
        when:
        def results = client.get("/health").json().get("results")

        then:
        results.fields().every { it.value.get("status").asText() && it.value.has("data") }
        DOWNSTREAM.every { results.get(it).get("description").asText() }
    }

    def "a known correlation id is echoed back on the response"() {
        given:
        def correlationId = UUID.randomUUID().toString()

        when:
        def response = client.get("/menu", ["X-Correlation-Id": correlationId])

        then:
        response.header("X-Correlation-Id") == correlationId
    }

    def "a correlation id is generated when the request omits one"() {
        when:
        def response = client.get("/menu")

        then:
        response.header("X-Correlation-Id")
    }

    def "the correlation id is propagated to downstream services"() {
        given:
        def correlationId = UUID.randomUUID().toString()

        when:
        client.get("/milk", ["X-Correlation-Id": correlationId])

        then:
        BreakfastBackends.cow().lastCorrelationId() == correlationId
    }

    def "a structured log entry is captured for order creation"() {
        given:
        Logger root = (Logger) LoggerFactory.getLogger(org.slf4j.Logger.ROOT_LOGGER_NAME)
        ListAppender<ILoggingEvent> appender = new ListAppender<>()
        appender.start()
        root.addAppender(appender)
        def customer = "Telemetry-${UUID.randomUUID()}"

        when:
        client.post("/orders", new OrderRequest(customer, [new OrderItemRequest("Pancakes", UUID.randomUUID(), 1)], 1))

        then:
        appender.list.any { e ->
            def msg = e.formattedMessage
            msg.contains("created for customer") && msg.contains(customer) && msg.contains("1 items")
        }

        cleanup:
        root.detachAppender(appender)
    }

    def "the health check reports degraded when downstream services are unreachable"() {
        given:
        BreakfastBackends.cow().setHealthStatus(503)
        BreakfastBackends.supplier().setHealthStatus(503)

        when:
        def body = client.get("/health").json()

        then:
        body.get("status").asText() == "Degraded"
        body.get("results").get("CowService").get("status").asText() == "Degraded"
        body.get("results").get("SupplierService").get("status").asText() == "Degraded"
    }

    def "the health check reports degraded when a downstream health endpoint errors"() {
        given:
        BreakfastBackends.kitchen().setHealthStatus(503)

        when:
        def body = client.get("/health").json()

        then:
        body.get("status").asText() == "Degraded"
        body.get("results").get("KitchenService").get("status").asText() == "Degraded"
    }
}
