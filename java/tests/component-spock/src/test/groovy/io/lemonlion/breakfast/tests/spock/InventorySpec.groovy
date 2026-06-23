package io.lemonlion.breakfast.tests.spock

import io.lemonlion.breakfast.BreakfastProviderApplication
import io.lemonlion.breakfast.model.request.InventoryItemRequest
import io.lemonlion.breakfast.model.response.InventoryItemResponse
import io.lemonlion.breakfast.testsupport.BackendsInitializer
import io.lemonlion.breakfast.testsupport.BreakfastBackends
import io.lemonlion.breakfast.testsupport.BreakfastTestClient
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.boot.test.web.server.LocalServerPort
import org.springframework.test.context.ContextConfiguration
import spock.lang.Specification

/** Inventory domain component spec (Spock). */
@SpringBootTest(classes = BreakfastProviderApplication, webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer)
class InventorySpec extends Specification {

    @LocalServerPort
    int port

    BreakfastTestClient client

    def setup() {
        client = new BreakfastTestClient("http://127.0.0.1:${port}")
        BreakfastBackends.resetFakes()
    }

    private static InventoryItemRequest valid() {
        new InventoryItemRequest("Flour", "Dry Goods", new BigDecimal("25.5"), "kg", new BigDecimal("5"))
    }

    def "an inventory item is created and retrievable"() {
        when:
        def created = client.post("/inventory", valid())

        then:
        created.status() == 201
        def item = created.as(InventoryItemResponse)
        item.id() > 0
        client.get("/inventory/${item.id()}").status() == 200
    }

    def "a negative quantity is rejected"() {
        when:
        def response = client.post("/inventory",
                new InventoryItemRequest("Flour", "Dry Goods", new BigDecimal("-1"), "kg", BigDecimal.ZERO))

        then:
        response.status() == 400
        response.bodyContains("'Quantity' must be greater than or equal to zero.")
    }
}
