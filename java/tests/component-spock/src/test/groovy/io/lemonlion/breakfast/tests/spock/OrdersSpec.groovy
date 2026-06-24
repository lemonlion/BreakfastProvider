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
}
