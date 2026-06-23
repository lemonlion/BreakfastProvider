package io.lemonlion.breakfast.tests.spock

import io.lemonlion.breakfast.BreakfastProviderApplication
import io.lemonlion.breakfast.model.request.PancakeRequest
import io.lemonlion.breakfast.model.response.PancakeResponse
import io.lemonlion.breakfast.testsupport.BackendsInitializer
import io.lemonlion.breakfast.testsupport.BreakfastBackends
import io.lemonlion.breakfast.testsupport.BreakfastTestClient
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.boot.test.web.server.LocalServerPort
import org.springframework.test.context.ContextConfiguration
import spock.lang.Specification

/** Pancakes domain component spec (Spock). */
@SpringBootTest(classes = BreakfastProviderApplication, webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer)
class PancakesSpec extends Specification {

    @LocalServerPort
    int port

    BreakfastTestClient client

    def setup() {
        client = new BreakfastTestClient("http://127.0.0.1:${port}")
        BreakfastBackends.kitchen().reset()
    }

    def "a valid pancake batch is created from its ingredients"() {
        when:
        def response = client.post("/pancakes", new PancakeRequest("Whole", "Plain", "Free-range", ["Syrup"]))

        then:
        response.status() == 201
        def batch = response.as(PancakeResponse)
        batch.batchId() != null
        batch.ingredients() == ["Whole", "Plain", "Free-range"]
    }

    def "a pancake request without milk is rejected"() {
        when:
        def response = client.post("/pancakes", new PancakeRequest(null, "Plain", "Free-range", []))

        then:
        response.status() == 400
        response.bodyContains("'Milk' is required.")
    }

    def "exceeding the topping limit is rejected"() {
        when:
        def response = client.post("/pancakes", new PancakeRequest("Whole", "Plain", "Free-range", ["a", "b", "c", "d", "e", "f"]))

        then:
        response.status() == 400
        response.bodyContains("Maximum toppings exceeded. Limit is 5.")
    }
}
