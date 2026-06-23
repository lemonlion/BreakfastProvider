package io.lemonlion.breakfast.tests.spock

import com.fasterxml.jackson.core.type.TypeReference
import io.lemonlion.breakfast.BreakfastProviderApplication
import io.lemonlion.breakfast.model.request.OrderItemRequest
import io.lemonlion.breakfast.model.request.OrderRequest
import io.lemonlion.breakfast.model.response.AuditLogResponse
import io.lemonlion.breakfast.model.response.OrderResponse
import io.lemonlion.breakfast.testsupport.BackendsInitializer
import io.lemonlion.breakfast.testsupport.BreakfastBackends
import io.lemonlion.breakfast.testsupport.BreakfastTestClient
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.boot.test.web.server.LocalServerPort
import org.springframework.test.context.ContextConfiguration
import spock.lang.Specification

/** AuditLogs domain component spec (Spock). */
@SpringBootTest(classes = BreakfastProviderApplication, webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer)
class AuditLogsSpec extends Specification {

    @LocalServerPort
    int port

    BreakfastTestClient client

    def setup() {
        client = new BreakfastTestClient("http://127.0.0.1:${port}")
        BreakfastBackends.resetFakes()
    }

    def "creating an order produces a queryable audit-log entry"() {
        given:
        def order = client.post("/orders",
                new OrderRequest("Alice", [new OrderItemRequest("Pancakes", UUID.randomUUID(), 1)], 3)).as(OrderResponse)

        when:
        def logs = client.get("/audit-logs?entityId=${order.orderId()}")

        then:
        logs.status() == 200
        logs.as(new TypeReference<List<AuditLogResponse>>() {}).any { it.action() == "Created" && it.entityType() == "Order" }
    }
}
