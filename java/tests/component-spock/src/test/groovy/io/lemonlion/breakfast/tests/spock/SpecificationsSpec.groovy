package io.lemonlion.breakfast.tests.spock

import io.lemonlion.breakfast.BreakfastProviderApplication
import io.lemonlion.breakfast.testsupport.BackendsInitializer
import io.lemonlion.breakfast.testsupport.BreakfastBackends
import io.lemonlion.breakfast.testsupport.BreakfastTestClient
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.boot.test.web.server.LocalServerPort
import org.springframework.test.context.ContextConfiguration
import spock.lang.Specification

/** Specifications component spec (Spock): OpenAPI document, Scalar UI, AsyncAPI document. */
@SpringBootTest(classes = BreakfastProviderApplication, webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer)
class SpecificationsSpec extends Specification {

    static final List<String> EXPECTED_PATHS = [
            "/pancakes", "/waffles", "/orders", "/orders/{orderId}", "/toppings",
            "/menu", "/milk", "/eggs", "/flour", "/goat-milk", "/audit-logs"]
    static final List<String> ASYNCAPI_KEYS =
            ["asyncapi", "info", "defaultContentType", "channels", "operations", "components"]

    @LocalServerPort
    int port

    BreakfastTestClient client

    def setup() {
        client = new BreakfastTestClient("http://127.0.0.1:${port}")
        BreakfastBackends.resetFakes()
    }

    def "the OpenAPI document is valid and contains all the endpoints"() {
        when:
        def response = client.get("/openapi/v1.json")

        then:
        response.status() == 200
        def paths = response.json().get("paths")
        EXPECTED_PATHS.every { paths.has(it) }
    }

    def "the Scalar UI endpoint returns a valid Scalar page"() {
        when:
        def response = client.get("/scalar/v1", ["Accept": "text/html"])

        then:
        response.status() == 200
        response.body().contains("<html")
        response.body().toLowerCase().contains("scalar")
    }

    def "the AsyncAPI document is valid and contains the expected top-level sections"() {
        when:
        def response = client.get("/asyncapi/v1.json")

        then:
        response.status() == 200
        def doc = response.json()
        ASYNCAPI_KEYS.every { doc.has(it) }
    }
}
