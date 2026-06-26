package io.lemonlion.breakfast.tests.spock

import io.lemonlion.breakfast.BreakfastProviderApplication
import io.lemonlion.breakfast.testsupport.BackendsInitializer
import io.lemonlion.breakfast.testsupport.BreakfastTestClient
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.boot.test.web.server.LocalServerPort
import org.springframework.test.context.ContextConfiguration
import org.springframework.test.context.TestPropertySource
import spock.lang.Specification

/** Goat-milk feature-flag scenario (Spock): flag disabled -> GET /goat-milk returns 404. */
@SpringBootTest(classes = BreakfastProviderApplication, webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer)
@TestPropertySource(properties = [
        "feature-switches.goat-milk-enabled=false",
        "grpc.server.in-process-name=breakfast-grpc-goatflag-spock"])
class GoatMilkFeatureFlagSpec extends Specification {

    @LocalServerPort
    int port

    BreakfastTestClient client

    def setup() {
        client = new BreakfastTestClient("http://127.0.0.1:${port}")
    }

    def "the goat-milk endpoint returns 404 when the feature is disabled"() {
        expect:
        client.get("/goat-milk").status() == 404
    }
}
