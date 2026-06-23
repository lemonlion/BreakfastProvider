package io.lemonlion.breakfast.tests.spock

import io.lemonlion.breakfast.BreakfastProviderApplication
import io.lemonlion.breakfast.testsupport.BackendsInitializer
import io.lemonlion.breakfast.testsupport.BreakfastBackends
import io.lemonlion.breakfast.testsupport.BreakfastTestClient
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
}
