package io.lemonlion.breakfast.tests.spock

import com.fasterxml.jackson.core.type.TypeReference
import io.lemonlion.breakfast.BreakfastProviderApplication
import io.lemonlion.breakfast.model.response.MenuItemResponse
import io.lemonlion.breakfast.testsupport.BackendsInitializer
import io.lemonlion.breakfast.testsupport.BreakfastBackends
import io.lemonlion.breakfast.testsupport.BreakfastTestClient
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.boot.test.web.server.LocalServerPort
import org.springframework.test.context.ContextConfiguration
import spock.lang.Specification

/** Menu domain component spec (Spock). */
@SpringBootTest(classes = BreakfastProviderApplication, webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer)
class MenuSpec extends Specification {

    @LocalServerPort
    int port

    BreakfastTestClient client

    def setup() {
        client = new BreakfastTestClient("http://127.0.0.1:${port}")
        BreakfastBackends.resetFakes()
    }

    def "the menu lists items as available when the supplier confirms ingredients"() {
        given:
        BreakfastBackends.supplier().setAvailabilityStatus(200)
        client.delete("/menu/cache")

        when:
        def response = client.get("/menu")

        then:
        response.status() == 200
        def menu = response.as(new TypeReference<List<MenuItemResponse>>() {})
        menu*.name().contains("Belgian Waffles")
        menu.every { it.isAvailable() }
    }

    def "when the supplier is down the menu items are marked unavailable"() {
        given:
        BreakfastBackends.supplier().setAvailabilityStatus(503)
        client.delete("/menu/cache")

        when:
        def response = client.get("/menu")

        then:
        response.status() == 200
        def menu = response.as(new TypeReference<List<MenuItemResponse>>() {})
        menu.every { !it.isAvailable() }
    }

    def "the menu cache can be cleared"() {
        expect:
        client.delete("/menu/cache").status() == 204
    }
}
