package io.lemonlion.breakfast.tests.spock

import com.fasterxml.jackson.core.type.TypeReference
import io.lemonlion.breakfast.BreakfastProviderApplication
import io.lemonlion.breakfast.model.response.ToppingResponse
import io.lemonlion.breakfast.testsupport.BackendsInitializer
import io.lemonlion.breakfast.testsupport.BreakfastTestClient
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.boot.test.web.server.LocalServerPort
import org.springframework.test.context.ContextConfiguration
import org.springframework.test.context.TestPropertySource
import spock.lang.Specification

/** Toppings feature-flag scenario (Spock): raspberry flag disabled -> catalogue excludes Raspberries. */
@SpringBootTest(classes = BreakfastProviderApplication, webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer)
@TestPropertySource(properties = [
        "feature-switches.raspberry-topping-enabled=false",
        "grpc.server.in-process-name=breakfast-grpc-toppingsflag-spock"])
class ToppingsFeatureFlagSpec extends Specification {

    @LocalServerPort
    int port

    BreakfastTestClient client

    def setup() {
        client = new BreakfastTestClient("http://127.0.0.1:${port}")
    }

    def "raspberries are excluded when the feature flag is disabled"() {
        when:
        def toppings = client.get("/toppings").as(new TypeReference<List<ToppingResponse>>() {})

        then:
        !toppings*.name().contains("Raspberries")
    }
}
