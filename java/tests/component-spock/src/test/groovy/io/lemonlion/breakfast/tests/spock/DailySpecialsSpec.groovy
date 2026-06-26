package io.lemonlion.breakfast.tests.spock

import com.fasterxml.jackson.core.type.TypeReference
import io.lemonlion.breakfast.BreakfastProviderApplication
import io.lemonlion.breakfast.model.request.DailySpecialOrderRequest
import io.lemonlion.breakfast.model.response.DailySpecialOrderResponse
import io.lemonlion.breakfast.model.response.DailySpecialResponse
import io.lemonlion.breakfast.testsupport.BackendsInitializer
import io.lemonlion.breakfast.testsupport.BreakfastBackends
import io.lemonlion.breakfast.testsupport.BreakfastTestClient
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.boot.test.web.server.LocalServerPort
import org.springframework.test.context.ContextConfiguration
import spock.lang.Specification

/** DailySpecials domain component spec (Spock). */
@SpringBootTest(classes = BreakfastProviderApplication, webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer)
class DailySpecialsSpec extends Specification {

    static final UUID SPECIAL = UUID.fromString("aaaa0000-0000-0000-0000-000000000001")
    static final UUID LEMON_RICOTTA = UUID.fromString("aaaa0000-0000-0000-0000-000000000003")
    static final int MAX_PER_SPECIAL = 100

    @LocalServerPort
    int port

    BreakfastTestClient client

    def setup() {
        client = new BreakfastTestClient("http://127.0.0.1:${port}")
        BreakfastBackends.resetFakes()
    }

    def "the available daily specials are listed"() {
        when:
        def response = client.get("/daily-specials")

        then:
        response.status() == 200
        def specials = response.as(new TypeReference<List<DailySpecialResponse>>() {})
        specials*.name().contains("Matcha Waffles")
    }

    def "ordering a special is idempotent under the same key"() {
        given:
        client.delete("/daily-specials/orders")
        def key = UUID.randomUUID().toString()
        def request = new DailySpecialOrderRequest(SPECIAL, 1)

        when:
        def first = client.post("/daily-specials/orders", request, ["Idempotency-Key": key]).as(DailySpecialOrderResponse)
        def repeat = client.post("/daily-specials/orders", request, ["Idempotency-Key": key])

        then:
        repeat.status() == 201
        repeat.as(DailySpecialOrderResponse).orderConfirmationId() == first.orderConfirmationId()
    }

    def "exceeding the daily limit returns 409"() {
        given:
        client.delete("/daily-specials/orders")

        expect:
        client.post("/daily-specials/orders", new DailySpecialOrderRequest(SPECIAL, 100)).status() == 201
        client.post("/daily-specials/orders", new DailySpecialOrderRequest(SPECIAL, 1)).status() == 409
    }

    def "ordering an unknown special returns 404"() {
        given:
        client.delete("/daily-specials/orders")

        expect:
        client.post("/daily-specials/orders", new DailySpecialOrderRequest(UUID.randomUUID(), 1)).status() == 404
    }

    def "a zero quantity is rejected"() {
        when:
        def response = client.post("/daily-specials/orders", new DailySpecialOrderRequest(SPECIAL, 0))

        then:
        response.status() == 400
        response.bodyContains("Quantity must be greater than zero.")
    }

    def "a valid daily special order returns a confirmation"() {
        given:
        client.delete("/daily-specials/orders")

        when:
        def response = client.post("/daily-specials/orders", new DailySpecialOrderRequest(SPECIAL, 1))

        then:
        response.status() == 201
        def body = response.as(DailySpecialOrderResponse)
        body.specialId() == SPECIAL
        body.orderConfirmationId() != null
    }

    def "the same order with two different idempotency keys returns different confirmations"() {
        given:
        client.delete("/daily-specials/orders")
        def request = new DailySpecialOrderRequest(SPECIAL, 1)

        when:
        def first = client.post("/daily-specials/orders", request, ["Idempotency-Key": UUID.randomUUID().toString()])
        def second = client.post("/daily-specials/orders", request, ["Idempotency-Key": UUID.randomUUID().toString()])

        then:
        first.status() == 201
        second.status() == 201
        second.as(DailySpecialOrderResponse).orderConfirmationId() != first.as(DailySpecialOrderResponse).orderConfirmationId()
    }

    def "the remaining quantity decreases after an order"() {
        given:
        client.delete("/daily-specials/orders")

        when:
        client.post("/daily-specials/orders", new DailySpecialOrderRequest(LEMON_RICOTTA, 1))
        def specials = client.get("/daily-specials").as(new TypeReference<List<DailySpecialResponse>>() {})
        def lemonRicotta = specials.find { it.specialId() == LEMON_RICOTTA }

        then:
        lemonRicotta.remainingQuantity() == MAX_PER_SPECIAL - 1
    }
}
