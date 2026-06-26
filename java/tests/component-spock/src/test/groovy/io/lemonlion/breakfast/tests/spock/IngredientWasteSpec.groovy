package io.lemonlion.breakfast.tests.spock

import com.fasterxml.jackson.core.type.TypeReference
import io.lemonlion.breakfast.BreakfastProviderApplication
import io.lemonlion.breakfast.model.request.IngredientWasteRequest
import io.lemonlion.breakfast.model.response.IngredientWasteResponse
import io.lemonlion.breakfast.testsupport.BackendsInitializer
import io.lemonlion.breakfast.testsupport.BreakfastBackends
import io.lemonlion.breakfast.testsupport.BreakfastTestClient
import org.awaitility.Awaitility
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.boot.test.web.server.LocalServerPort
import org.springframework.test.context.ContextConfiguration
import spock.lang.Specification

import java.time.Duration

/** IngredientWaste domain component spec (Spock) — BigQuery analytics. */
@SpringBootTest(classes = BreakfastProviderApplication, webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer)
class IngredientWasteSpec extends Specification {

    @LocalServerPort
    int port

    BreakfastTestClient client

    def setup() {
        client = new BreakfastTestClient("http://127.0.0.1:${port}")
        BreakfastBackends.resetFakes()
    }

    def "recording waste returns the created record"() {
        when:
        def recorded = client.post("/ingredient-waste",
                new IngredientWasteRequest("Flour", new BigDecimal("0.5"), "kg", "Pancakes", "Burnt batch"))

        then:
        recorded.status() == 201
        recorded.as(IngredientWasteResponse).wasteId()
    }

    def "waste is queryable by recipe"() {
        given:
        def recipe = "Pancakes-${UUID.randomUUID()}"
        client.post("/ingredient-waste",
                new IngredientWasteRequest("Flour", new BigDecimal("0.5"), "kg", recipe, "Burnt batch"))

        expect:
        Awaitility.await().atMost(Duration.ofSeconds(20)).untilAsserted {
            def listed = client.get("/ingredient-waste/recipe/${recipe}")
            assert listed.status() == 200
            assert listed.as(new TypeReference<List<IngredientWasteResponse>>() {}).any { it.recipeName() == recipe }
        }
    }

    def "a missing reason is rejected"() {
        when:
        def response = client.post("/ingredient-waste",
                new IngredientWasteRequest("Flour", new BigDecimal("0.5"), "kg", "Pancakes", null))

        then:
        response.status() == 400
        response.bodyContains("'Reason' must not be empty.")
    }

    def "a missing ingredient name is rejected"() {
        when:
        def response = client.post("/ingredient-waste",
                new IngredientWasteRequest(null, new BigDecimal("0.5"), "kg", "Pancakes", "Spill"))

        then:
        response.status() == 400
        response.bodyContains("'Ingredient Name' must not be empty.")
    }

    def "a zero quantity is rejected"() {
        when:
        def response = client.post("/ingredient-waste",
                new IngredientWasteRequest("Flour", BigDecimal.ZERO, "kg", "Pancakes", "Spill"))

        then:
        response.status() == 400
        response.bodyContains("'Quantity Wasted' must be greater than zero.")
    }

    def "deleting a waste record returns 204"() {
        given:
        def waste = client.post("/ingredient-waste",
                new IngredientWasteRequest("Flour", new BigDecimal("0.5"), "kg", "Pancakes", "Spill")).as(IngredientWasteResponse)

        expect:
        client.delete("/ingredient-waste/${waste.wasteId()}").status() == 204
    }
}
