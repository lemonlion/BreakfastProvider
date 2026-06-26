package io.lemonlion.breakfast.tests.spock

import io.lemonlion.breakfast.BreakfastProviderApplication
import io.lemonlion.breakfast.model.request.OrderItemRequest
import io.lemonlion.breakfast.model.request.OrderRequest
import io.lemonlion.breakfast.model.request.PancakeRequest
import io.lemonlion.breakfast.model.response.PancakeResponse
import io.lemonlion.breakfast.testsupport.BackendsInitializer
import org.awaitility.Awaitility
import java.time.Duration
import io.lemonlion.breakfast.testsupport.BreakfastBackends
import io.lemonlion.breakfast.testsupport.BreakfastTestClient
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.boot.test.web.server.LocalServerPort
import org.springframework.test.context.ContextConfiguration
import spock.lang.Specification

/** Reporting domain component spec (Spock) — GraphQL order summaries. */
@SpringBootTest(classes = BreakfastProviderApplication, webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer)
class ReportingSpec extends Specification {

    @LocalServerPort
    int port

    BreakfastTestClient client

    def setup() {
        client = new BreakfastTestClient("http://127.0.0.1:${port}")
        BreakfastBackends.resetFakes()
    }

    def "a created order appears in the GraphQL order summaries"() {
        given:
        def customer = "Cust-${UUID.randomUUID()}"
        client.post("/orders", new OrderRequest(customer, [new OrderItemRequest("Pancakes", UUID.randomUUID(), 2)], 4))

        expect:
        // Query path reads through a separate request/session; poll so a Cosmos cross-request
        // read-after-write lag under host load doesn't flake the single assert.
        Awaitility.await().atMost(Duration.ofSeconds(20)).untilAsserted {
            def gql = client.post("/graphql", [query: "{ orderSummaries { orderId customerName itemCount } }"])
            assert gql.status() == 200
            assert gql.bodyContains(customer)
        }
    }

    def "popular recipes reflects the ordered recipe types"() {
        given:
        client.post("/orders", new OrderRequest("Recipe-${UUID.randomUUID()}",
                [new OrderItemRequest("Pancakes", UUID.randomUUID(), 2)], 4))

        expect:
        Awaitility.await().atMost(Duration.ofSeconds(20)).untilAsserted {
            def gql = client.post("/graphql", [query: "{ popularRecipes { recipeType count } }"])
            assert gql.status() == 200
            assert gql.bodyContains("Pancakes")
        }
    }

    def "an ingredient delivery posted to the EventGrid webhook appears in ingredient shipments"() {
        given:
        def deliveryId = UUID.randomUUID().toString()
        def event = [
                id       : UUID.randomUUID().toString(),
                eventType: "IngredientDeliveryEvent",
                subject  : "supply-chain/deliveries",
                data     : [deliveryId: deliveryId, ingredientName: "Milk", quantity: 50.0,
                            deliveredAt: java.time.Instant.now().toString()]]

        and:
        def webhook = client.post("/webhooks/eventgrid", [event])

        expect:
        webhook.status() == 200

        and:
        Awaitility.await().atMost(Duration.ofSeconds(20)).untilAsserted {
            def gql = client.post("/graphql", [query: "{ ingredientShipments { deliveryId ingredientName quantity } }"])
            assert gql.status() == 200
            assert gql.bodyContains(deliveryId)
            assert gql.bodyContains("Milk")
        }
    }

    def "a completed pancake batch is ingested into batch completions via Pub/Sub"() {
        given:
        def batch = client.post("/pancakes",
                new PancakeRequest("Whole", "Plain", "Free-range", ["Syrup"])).as(PancakeResponse)
        def batchId = batch.batchId().toString()

        expect:
        Awaitility.await().atMost(Duration.ofSeconds(20)).untilAsserted {
            def gql = client.post("/graphql", [query: "{ batchCompletions { batchId recipeType } }"])
            assert gql.status() == 200
            assert gql.bodyContains(batchId)
        }
    }

    def "a batch's equipment alert flows through Event Hubs into equipmentAlerts"() {
        given:
        def batch = client.post("/pancakes",
                new PancakeRequest("Whole", "Plain", "Free-range", ["Syrup"])).as(PancakeResponse)
        def batchId = batch.batchId().toString()

        expect:
        Awaitility.await().atMost(Duration.ofSeconds(40)).untilAsserted {
            def gql = client.post("/graphql",
                    [query: "{ equipmentAlerts { alertId batchId equipmentName alertType } }"])
            assert gql.status() == 200
            assert gql.bodyContains(batchId)
        }
    }
}
