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
}
