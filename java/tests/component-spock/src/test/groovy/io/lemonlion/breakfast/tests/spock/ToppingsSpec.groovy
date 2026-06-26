package io.lemonlion.breakfast.tests.spock

import com.fasterxml.jackson.core.type.TypeReference
import io.lemonlion.breakfast.BreakfastProviderApplication
import io.lemonlion.breakfast.model.request.ToppingRequest
import io.lemonlion.breakfast.model.request.UpdateToppingRequest
import io.lemonlion.breakfast.model.response.ToppingResponse
import io.lemonlion.breakfast.testsupport.BackendsInitializer
import io.lemonlion.breakfast.testsupport.BreakfastBackends
import io.lemonlion.breakfast.testsupport.BreakfastTestClient
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.boot.test.web.server.LocalServerPort
import org.springframework.test.context.ContextConfiguration
import spock.lang.Specification

/** Toppings domain component spec (Spock). */
@SpringBootTest(classes = BreakfastProviderApplication, webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer)
class ToppingsSpec extends Specification {

    static final UUID SEEDED = UUID.fromString("11111111-0000-0000-0000-000000000003")

    @LocalServerPort
    int port

    BreakfastTestClient client

    def setup() {
        client = new BreakfastTestClient("http://127.0.0.1:${port}")
        BreakfastBackends.kitchen().reset()
    }

    def "the topping catalogue is returned"() {
        when:
        def response = client.get("/toppings")

        then:
        response.status() == 200
        def toppings = response.as(new TypeReference<List<ToppingResponse>>() {})
        toppings*.name().contains("Maple Syrup")
    }

    def "a new topping is created and gets an id"() {
        when:
        def response = client.post("/toppings", new ToppingRequest("Caramel", "Syrup"))

        then:
        response.status() == 201
        response.as(ToppingResponse).toppingId() != null
    }

    def "a topping without a name is rejected"() {
        when:
        def response = client.post("/toppings", new ToppingRequest(null, "Syrup"))

        then:
        response.status() == 400
        response.bodyContains("'Name' is required.")
    }

    def "updating an existing topping returns the updated topping"() {
        when:
        def updated = client.put("/toppings/${SEEDED}", new UpdateToppingRequest("Golden Syrup", "Syrup"))

        then:
        updated.status() == 200
        updated.as(ToppingResponse).name() == "Golden Syrup"
    }

    def "updating a non-existent topping returns 404"() {
        expect:
        client.put("/toppings/${UUID.randomUUID()}", new UpdateToppingRequest("X", "Y")).status() == 404
    }

    def "deleting an existing topping returns 204"() {
        expect:
        client.delete("/toppings/${SEEDED}").status() == 204
    }

    def "deleting a non-existent topping returns 404"() {
        expect:
        client.delete("/toppings/${UUID.randomUUID()}").status() == 404
    }

    def "updating a topping with HTML/script content is rejected"() {
        when:
        def response = client.put("/toppings/${SEEDED}", new UpdateToppingRequest("<script>alert(1)</script>", "Syrup"))

        then:
        response.status() == 400
        response.bodyContains("must not contain HTML or script content.")
    }

    def "raspberries are included when the feature flag is enabled"() {
        when:
        def toppings = client.get("/toppings").as(new TypeReference<List<ToppingResponse>>() {})

        then:
        toppings*.name().contains("Raspberries")
    }

    def "a topping name with HTML/script content is rejected"() {
        when:
        def response = client.post("/toppings", new ToppingRequest("<script>alert(1)</script>", "Syrup"))

        then:
        response.status() == 400
        response.bodyContains("must not contain HTML or script content.")
    }
}
