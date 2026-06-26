package io.lemonlion.breakfast.tests.spock

import io.lemonlion.breakfast.BreakfastProviderApplication
import io.lemonlion.breakfast.model.request.OrderItemRequest
import io.lemonlion.breakfast.model.request.OrderRequest
import io.lemonlion.breakfast.model.request.UpdateOrderStatusRequest
import io.lemonlion.breakfast.model.response.OrderResponse
import io.lemonlion.breakfast.testsupport.BackendsInitializer
import io.lemonlion.breakfast.testsupport.BreakfastBackends
import io.lemonlion.breakfast.testsupport.BreakfastTestClient
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.boot.test.web.server.LocalServerPort
import org.springframework.test.context.ContextConfiguration
import spock.lang.Specification

/** Orders domain component spec (Spock) — same behaviour as the other framework suites. */
@SpringBootTest(classes = BreakfastProviderApplication, webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer)
class OrdersSpec extends Specification {

    @LocalServerPort
    int port

    BreakfastTestClient client

    def setup() {
        client = new BreakfastTestClient("http://127.0.0.1:${port}")
        BreakfastBackends.kitchen().reset()
    }

    private static OrderRequest validOrder() {
        new OrderRequest("Alice", [new OrderItemRequest("Pancakes", UUID.randomUUID(), 2)], 5)
    }

    def "a valid order is created and the kitchen is notified"() {
        when:
        def response = client.post("/orders", validOrder())

        then:
        response.status() == 201
        def order = response.as(OrderResponse)
        order.customerName() == "Alice"
        order.status() == "Created"
        BreakfastBackends.kitchen().receivedPreparation()
    }

    def "retrieving a non-existent order returns 404"() {
        expect:
        client.get("/orders/${UUID.randomUUID()}").status() == 404
    }

    def "a valid status transition updates the order"() {
        given:
        def order = client.post("/orders", validOrder()).as(OrderResponse)

        when:
        def updated = client.patch("/orders/${order.orderId()}/status", new UpdateOrderStatusRequest("Preparing"))

        then:
        updated.status() == 200
        updated.as(OrderResponse).status() == "Preparing"
    }

    def "an invalid status transition returns 409"() {
        given:
        def order = client.post("/orders", validOrder()).as(OrderResponse)

        expect:
        client.patch("/orders/${order.orderId()}/status", new UpdateOrderStatusRequest("Ready")).status() == 409
    }

    def "an order without a customer name is rejected"() {
        given:
        def invalid = new OrderRequest(null, [new OrderItemRequest("Pancakes", UUID.randomUUID(), 1)], 1)

        when:
        def response = client.post("/orders", invalid)

        then:
        response.status() == 400
        response.bodyContains("'Customer Name' is required.")
    }

    def "an order moves through its complete lifecycle to Completed"() {
        given:
        def id = client.post("/orders", validOrder()).as(OrderResponse).orderId().toString()

        expect:
        client.patch("/orders/${id}/status", new UpdateOrderStatusRequest("Preparing")).status() == 200
        client.patch("/orders/${id}/status", new UpdateOrderStatusRequest("Ready")).status() == 200

        when:
        def completed = client.patch("/orders/${id}/status", new UpdateOrderStatusRequest("Completed"))

        then:
        completed.status() == 200
        completed.as(OrderResponse).status() == "Completed"
    }

    def "an order is still created when the kitchen service fails"() {
        given:
        BreakfastBackends.kitchen().setNextStatus(503)

        when:
        def response = client.post("/orders", validOrder())

        then:
        response.status() == 201
        response.as(OrderResponse).customerName() == "Alice"
    }

    def "an order exceeding the item limit is rejected with 400"() {
        given:
        def items = (1..11).collect { new OrderItemRequest("Pancakes", UUID.randomUUID(), 1) }

        when:
        def response = client.post("/orders", new OrderRequest("Alice", items, 1))

        then:
        response.status() == 400
        response.bodyContains("cannot contain more than 10 items")
    }

    def "orders are returned with pagination metadata"() {
        given:
        def customer = "Page-${UUID.randomUUID()}"
        client.post("/orders", new OrderRequest(customer, [new OrderItemRequest("Pancakes", UUID.randomUUID(), 1)], 1))
        client.post("/orders", new OrderRequest(customer, [new OrderItemRequest("Pancakes", UUID.randomUUID(), 1)], 2))

        when:
        def page = client.get("/orders?page=1&pageSize=1")

        then:
        page.status() == 200
        def body = page.json()
        body.get("page").asInt() == 1
        body.get("pageSize").asInt() == 1
        body.get("items").size() == 1
        body.get("totalCount").asInt() >= 2
    }

    def "a created order can be cancelled"() {
        given:
        def order = client.post("/orders", validOrder()).as(OrderResponse)

        when:
        def cancelled = client.patch("/orders/${order.orderId()}/status", new UpdateOrderStatusRequest("Cancelled"))

        then:
        cancelled.status() == 200
        cancelled.as(OrderResponse).status() == "Cancelled"
    }

    def "an order at the maximum item limit is accepted"() {
        given:
        def items = (1..10).collect { new OrderItemRequest("Pancakes", UUID.randomUUID(), 1) }

        when:
        def response = client.post("/orders", new OrderRequest("Alice", items, 1))

        then:
        response.status() == 201
        response.as(OrderResponse).items().size() == 10
    }

    def "the second page of orders returns different results"() {
        given:
        def customer = "Page2-${UUID.randomUUID()}"
        client.post("/orders", new OrderRequest(customer, [new OrderItemRequest("Pancakes", UUID.randomUUID(), 1)], 1))
        client.post("/orders", new OrderRequest(customer, [new OrderItemRequest("Pancakes", UUID.randomUUID(), 1)], 2))

        when:
        def page1 = client.get("/orders?page=1&pageSize=1").json()
        def page2 = client.get("/orders?page=2&pageSize=1").json()

        then:
        page2.get("page").asInt() == 2
        page2.get("items").size() == 1
        page2.get("items").get(0).get("orderId").asText() != page1.get("items").get(0).get("orderId").asText()
    }

    def "an order without items is rejected"() {
        when:
        def response = client.post("/orders", new OrderRequest("Alice", [], 1))

        then:
        response.status() == 400
        response.bodyContains("The Items field is required.")
    }

    def "creating an order writes a Created audit log entry"() {
        given:
        def order = client.post("/orders", validOrder()).as(OrderResponse)

        when:
        def audit = client.get("/audit-logs?entityType=Order&entityId=${order.orderId()}")

        then:
        audit.status() == 200
        audit.bodyContains("Created")
        audit.bodyContains(order.orderId().toString())
    }
}
