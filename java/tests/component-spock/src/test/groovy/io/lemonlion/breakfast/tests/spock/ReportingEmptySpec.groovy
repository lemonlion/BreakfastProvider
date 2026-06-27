package io.lemonlion.breakfast.tests.spock

import com.fasterxml.jackson.databind.JsonNode
import io.lemonlion.breakfast.BreakfastProviderApplication
import io.lemonlion.breakfast.testsupport.BreakfastTestClient
import io.lemonlion.breakfast.testsupport.EmptyReportingBackendsInitializer
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.boot.test.web.server.LocalServerPort
import org.springframework.test.context.ContextConfiguration
import spock.lang.Specification

/** Order-summaries-empty scenario (Spock): isolated empty H2 reporting store, no orders -> empty list. */
@SpringBootTest(classes = BreakfastProviderApplication, webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = EmptyReportingBackendsInitializer)
class ReportingEmptySpec extends Specification {

    @LocalServerPort
    int port

    def "order summaries return an empty list when no orders exist"() {
        given:
        def client = new BreakfastTestClient("http://127.0.0.1:${port}")

        when:
        def gql = client.post("/graphql", [query: "{ orderSummaries { orderId } }"])

        then:
        gql.status() == 200
        JsonNode summaries = gql.json().get("data").get("orderSummaries")
        summaries.isArray()
        summaries.size() == 0
    }
}
