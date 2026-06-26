package io.lemonlion.breakfast.tests.spock

import io.lemonlion.breakfast.BreakfastProviderApplication
import io.lemonlion.breakfast.model.request.CustomerPreferenceRequest
import io.lemonlion.breakfast.model.response.CustomerPreferenceResponse
import io.lemonlion.breakfast.testsupport.BackendsInitializer
import io.lemonlion.breakfast.testsupport.BreakfastBackends
import io.lemonlion.breakfast.testsupport.BreakfastTestClient
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.boot.test.web.server.LocalServerPort
import org.springframework.test.context.ContextConfiguration
import spock.lang.Specification

/** CustomerPreferences domain component spec (Spock) — Spanner persistence. */
@SpringBootTest(classes = BreakfastProviderApplication, webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer)
class CustomerPreferencesSpec extends Specification {

    @LocalServerPort
    int port

    BreakfastTestClient client

    def setup() {
        client = new BreakfastTestClient("http://127.0.0.1:${port}")
        BreakfastBackends.resetFakes()
    }

    def "a preference is upserted and then retrievable"() {
        given:
        def customerId = "cust-${UUID.randomUUID()}"

        when:
        def upserted = client.put("/customer-preferences/${customerId}",
                new CustomerPreferenceRequest(null, "Alice", "oat", true, "Pancakes"))

        then:
        upserted.status() == 200
        upserted.as(CustomerPreferenceResponse).preferredMilkType() == "oat"
        client.get("/customer-preferences/${customerId}").status() == 200
    }

    def "saving preferences returns the saved preferences"() {
        given:
        def customerId = "cust-${UUID.randomUUID()}"

        when:
        def saved = client.put("/customer-preferences/${customerId}",
                new CustomerPreferenceRequest(null, "Alice", "oat", true, "Pancakes"))

        then:
        saved.status() == 200
        def pref = saved.as(CustomerPreferenceResponse)
        pref.customerId() == customerId
        pref.preferredMilkType() == "oat"
    }

    def "updating preferences returns the updated preferences"() {
        given:
        def customerId = "cust-${UUID.randomUUID()}"
        client.put("/customer-preferences/${customerId}",
                new CustomerPreferenceRequest(null, "Alice", "oat", true, "Pancakes"))

        when:
        def updated = client.put("/customer-preferences/${customerId}",
                new CustomerPreferenceRequest(null, "Alice", "almond", false, "Waffles"))

        then:
        updated.status() == 200
        updated.as(CustomerPreferenceResponse).preferredMilkType() == "almond"
    }

    def "retrieving an unknown customer returns 404"() {
        expect:
        client.get("/customer-preferences/unknown-${UUID.randomUUID()}").status() == 404
    }

    def "an empty customer name is rejected"() {
        when:
        def response = client.put("/customer-preferences/cust-x",
                new CustomerPreferenceRequest(null, "", "oat", false, ""))

        then:
        response.status() == 400
        response.bodyContains("'Customer Name' must not be empty.")
    }
}
