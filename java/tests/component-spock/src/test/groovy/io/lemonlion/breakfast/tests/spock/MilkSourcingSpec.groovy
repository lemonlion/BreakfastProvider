package io.lemonlion.breakfast.tests.spock

import io.lemonlion.breakfast.BreakfastProviderApplication
import io.lemonlion.breakfast.model.response.GoatMilkResponse
import io.lemonlion.breakfast.model.response.MilkResponse
import io.lemonlion.breakfast.testsupport.BackendsInitializer
import io.lemonlion.breakfast.testsupport.BreakfastBackends
import io.lemonlion.breakfast.testsupport.BreakfastTestClient
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.boot.test.web.server.LocalServerPort
import org.springframework.test.context.ContextConfiguration
import spock.lang.Specification

/** Milk-sourcing domain component spec (Spock) — Cow/Goat HTTP downstream. */
@SpringBootTest(classes = BreakfastProviderApplication, webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer)
class MilkSourcingSpec extends Specification {

    @LocalServerPort
    int port

    BreakfastTestClient client

    def setup() {
        client = new BreakfastTestClient("http://127.0.0.1:${port}")
        BreakfastBackends.resetFakes()
    }

    def "milk is sourced from the cow service"() {
        when:
        def response = client.get("/milk")

        then:
        response.status() == 200
        response.as(MilkResponse).milk() == "Some_Milk"
    }

    def "goat milk is sourced when the feature is enabled"() {
        when:
        def response = client.get("/goat-milk")

        then:
        response.status() == 200
        response.as(GoatMilkResponse).goatMilk() == "Some_Fresh_Goat_Milk"
    }

    def "a cow service failure returns 502"() {
        given:
        BreakfastBackends.cow().setStatus(503)

        expect:
        client.get("/milk").status() == 502
    }
}
