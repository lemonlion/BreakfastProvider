package io.lemonlion.breakfast.tests.spock

import com.fasterxml.jackson.core.type.TypeReference
import io.lemonlion.breakfast.BreakfastProviderApplication
import io.lemonlion.breakfast.model.request.OrderItemRequest
import io.lemonlion.breakfast.model.request.OrderRequest
import io.lemonlion.breakfast.model.response.ToppingResponse
import io.lemonlion.breakfast.testsupport.BackendsInitializer
import io.lemonlion.breakfast.testsupport.BreakfastBackends
import io.lemonlion.breakfast.testsupport.BreakfastTestClient
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.boot.test.web.server.LocalServerPort
import org.springframework.test.context.ContextConfiguration
import org.springframework.test.context.TestPropertySource
import spock.lang.Specification

/**
 * Configuration-override scenarios (Spock), consolidated into a single extra Spring context to keep the
 * number of heavyweight backend-bearing contexts small. Covers the C# Rate_Limiting, Toppings Feature_Flag
 * (disabled) and Ingredients Goat_Milk_Feature_Flag (disabled) scenarios; only the rate-limit feature
 * creates orders, so the overrides don't interfere.
 */
@SpringBootTest(classes = BreakfastProviderApplication, webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer)
@TestPropertySource(properties = [
        "rate-limit.permit-limit=1",
        "rate-limit.window-seconds=60",
        "feature-switches.raspberry-topping-enabled=false",
        "feature-switches.goat-milk-enabled=false",
        "grpc.server.in-process-name=breakfast-grpc-overrides-spock"])
class OverridesSpec extends Specification {

    @LocalServerPort
    int port

    BreakfastTestClient client

    def setup() {
        client = new BreakfastTestClient("http://127.0.0.1:${port}")
        BreakfastBackends.resetFakes()
    }

    def "a second order within the window is rate limited with 429"() {
        given:
        def order = new OrderRequest("RateLimit-${UUID.randomUUID()}",
                [new OrderItemRequest("Pancakes", UUID.randomUUID(), 1)], 1)

        expect:
        client.post("/orders", order).status() == 201
        client.post("/orders", order).status() == 429
    }

    def "raspberries are excluded when the feature flag is disabled"() {
        when:
        def toppings = client.get("/toppings").as(new TypeReference<List<ToppingResponse>>() {})

        then:
        !toppings*.name().contains("Raspberries")
    }

    def "the goat-milk endpoint returns 404 when the feature is disabled"() {
        expect:
        client.get("/goat-milk").status() == 404
    }
}
