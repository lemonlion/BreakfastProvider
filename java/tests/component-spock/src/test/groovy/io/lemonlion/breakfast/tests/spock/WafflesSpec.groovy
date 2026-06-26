package io.lemonlion.breakfast.tests.spock

import io.lemonlion.breakfast.BreakfastProviderApplication
import io.lemonlion.breakfast.model.request.WaffleRequest
import io.lemonlion.breakfast.model.response.WaffleResponse
import io.lemonlion.breakfast.testsupport.BackendsInitializer
import io.lemonlion.breakfast.testsupport.BreakfastBackends
import io.lemonlion.breakfast.testsupport.BreakfastTestClient
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.boot.test.web.server.LocalServerPort
import org.springframework.test.context.ContextConfiguration
import spock.lang.Specification

/** Waffles domain component spec (Spock). */
@SpringBootTest(classes = BreakfastProviderApplication, webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer)
class WafflesSpec extends Specification {

    @LocalServerPort
    int port

    BreakfastTestClient client

    def setup() {
        client = new BreakfastTestClient("http://127.0.0.1:${port}")
        BreakfastBackends.kitchen().reset()
    }

    def "a valid waffle batch includes butter among the ingredients"() {
        when:
        def response = client.post("/waffles", new WaffleRequest("Whole", "Plain", "Free-range", "Salted", ["Syrup"]))

        then:
        response.status() == 201
        def batch = response.as(WaffleResponse)
        batch.batchId() != null
        batch.ingredients() == ["Whole", "Plain", "Free-range", "Salted"]
    }

    def "a waffle request without butter is rejected"() {
        when:
        def response = client.post("/waffles", new WaffleRequest("Whole", "Plain", "Free-range", null, []))

        then:
        response.status() == 400
        response.bodyContains("'Butter' is required.")
    }

    def "exceeding the topping limit is rejected"() {
        when:
        def response = client.post("/waffles",
                new WaffleRequest("Whole", "Plain", "Free-range", "Salted", ["a", "b", "c", "d", "e", "f"]))

        then:
        response.status() == 400
        response.bodyContains("Maximum toppings exceeded. Limit is 5.")
    }

    def "an unsupported content type is rejected with 415"() {
        expect:
        client.postRaw("/waffles", "Whole Plain Free-range Salted", "text/plain").status() == 415
    }
}
