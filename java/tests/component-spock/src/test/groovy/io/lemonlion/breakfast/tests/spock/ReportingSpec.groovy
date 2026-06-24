package io.lemonlion.breakfast.tests.spock

import io.lemonlion.breakfast.BreakfastProviderApplication
import io.lemonlion.breakfast.model.request.OrderItemRequest
import io.lemonlion.breakfast.model.request.OrderRequest
import io.lemonlion.breakfast.model.event.EquipmentAlertEvent
import io.lemonlion.breakfast.model.request.PancakeRequest
import io.lemonlion.breakfast.model.response.PancakeResponse
import io.lemonlion.breakfast.reporting.EquipmentAlertConsumer
import io.lemonlion.breakfast.testsupport.BackendsInitializer
import org.awaitility.Awaitility
import org.springframework.beans.factory.annotation.Autowired
import java.time.Duration
import java.time.Instant
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

    @Autowired
    EquipmentAlertConsumer equipmentAlertConsumer

    BreakfastTestClient client

    def setup() {
        client = new BreakfastTestClient("http://127.0.0.1:${port}")
        BreakfastBackends.resetFakes()
    }

    def "a created order appears in the GraphQL order summaries"() {
        given:
        def customer = "Cust-${UUID.randomUUID()}"
        client.post("/orders", new OrderRequest(customer, [new OrderItemRequest("Pancakes", UUID.randomUUID(), 2)], 4))

        when:
        def gql = client.post("/graphql", [query: "{ orderSummaries { orderId customerName itemCount } }"])

        then:
        gql.status() == 200
        gql.bodyContains(customer)
    }

    def "popular recipes reflects the ordered recipe types"() {
        given:
        client.post("/orders", new OrderRequest("Recipe-${UUID.randomUUID()}",
                [new OrderItemRequest("Pancakes", UUID.randomUUID(), 2)], 4))

        when:
        def gql = client.post("/graphql", [query: "{ popularRecipes { recipeType count } }"])

        then:
        gql.status() == 200
        gql.bodyContains("Pancakes")
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

        when:
        def webhook = client.post("/webhooks/eventgrid", [event])

        then:
        webhook.status() == 200

        when:
        def gql = client.post("/graphql", [query: "{ ingredientShipments { deliveryId ingredientName quantity } }"])

        then:
        gql.status() == 200
        gql.bodyContains(deliveryId)
        gql.bodyContains("Milk")
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

    def "an equipment alert is ingested and surfaced via the GraphQL query"() {
        given:
        def alertId = UUID.randomUUID()
        equipmentAlertConsumer.ingest(new EquipmentAlertEvent(
                alertId, UUID.randomUUID(), "Griddle", "UsageCycleCompleted", Instant.now()))

        when:
        def gql = client.post("/graphql", [query: "{ equipmentAlerts { alertId equipmentName alertType } }"])

        then:
        gql.status() == 200
        gql.bodyContains(alertId.toString())
    }
}
