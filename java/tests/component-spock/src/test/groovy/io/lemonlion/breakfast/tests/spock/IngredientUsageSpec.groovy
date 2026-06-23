package io.lemonlion.breakfast.tests.spock

import com.fasterxml.jackson.core.type.TypeReference
import io.lemonlion.breakfast.BreakfastProviderApplication
import io.lemonlion.breakfast.model.request.IngredientUsageRequest
import io.lemonlion.breakfast.model.response.IngredientUsageResponse
import io.lemonlion.breakfast.testsupport.BackendsInitializer
import io.lemonlion.breakfast.testsupport.BreakfastBackends
import io.lemonlion.breakfast.testsupport.BreakfastTestClient
import org.awaitility.Awaitility
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.boot.test.web.server.LocalServerPort
import org.springframework.test.context.ContextConfiguration
import spock.lang.Specification

import java.time.Duration

/** IngredientUsage domain component spec (Spock) — BigQuery analytics. */
@SpringBootTest(classes = BreakfastProviderApplication, webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer)
class IngredientUsageSpec extends Specification {

    @LocalServerPort
    int port

    BreakfastTestClient client

    def setup() {
        client = new BreakfastTestClient("http://127.0.0.1:${port}")
        BreakfastBackends.resetFakes()
    }

    def "usage is recorded and queryable by ingredient"() {
        given:
        def ingredient = "Flour-${UUID.randomUUID()}"

        when:
        def recorded = client.post("/ingredient-usage",
                new IngredientUsageRequest(ingredient, new BigDecimal("2.5"), "kg", "Classic Pancakes"))

        then:
        recorded.status() == 201
        Awaitility.await().atMost(Duration.ofSeconds(20)).untilAsserted {
            def listed = client.get("/ingredient-usage/ingredient/${ingredient}")
            assert listed.status() == 200
            assert listed.as(new TypeReference<List<IngredientUsageResponse>>() {}).any { it.ingredientName() == ingredient }
        }
    }

    def "a non-positive quantity is rejected"() {
        when:
        def response = client.post("/ingredient-usage",
                new IngredientUsageRequest("Flour", BigDecimal.ZERO, "kg", "Pancakes"))

        then:
        response.status() == 400
        response.bodyContains("'Quantity Used' must be greater than zero.")
    }
}
