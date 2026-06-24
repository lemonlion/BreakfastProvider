package io.lemonlion.breakfast.tests.spock

import io.lemonlion.breakfast.BreakfastProviderApplication
import io.lemonlion.breakfast.model.request.OrderItemRequest
import io.lemonlion.breakfast.model.request.OrderRequest
import io.lemonlion.breakfast.testsupport.BackendsInitializer
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
}
