package io.lemonlion.breakfast.tests.spock

import io.lemonlion.breakfast.BreakfastProviderApplication
import io.lemonlion.breakfast.model.request.OrderItemRequest
import io.lemonlion.breakfast.model.request.OrderRequest
import io.lemonlion.breakfast.testsupport.BackendsInitializer
import io.lemonlion.breakfast.testsupport.BreakfastBackends
import io.lemonlion.breakfast.testsupport.BreakfastTestClient
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.boot.test.web.server.LocalServerPort
import org.springframework.test.context.ContextConfiguration
import org.springframework.test.context.TestPropertySource
import spock.lang.Specification

/**
 * Orders rate-limiting (Spock). Own Spring context with the permit limit overridden to 1 (twin of the
 * C# Rate_Limiting scenario) so the second order within the window is rejected with 429. A unique
 * in-process gRPC name keeps this extra context from colliding with the shared one.
 */
@SpringBootTest(classes = BreakfastProviderApplication, webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer)
@TestPropertySource(properties = [
        "rate-limit.permit-limit=1",
        "rate-limit.window-seconds=60",
        "grpc.server.in-process-name=breakfast-grpc-ratelimit-spock"])
class OrdersRateLimitSpec extends Specification {

    @LocalServerPort
    int port

    BreakfastTestClient client

    def setup() {
        client = new BreakfastTestClient("http://127.0.0.1:${port}")
        BreakfastBackends.resetFakes()
    }

    def "a second order within the window is rate limited with 429"() {
        given:
        def order = new OrderRequest("RateLimit-${UUID.randomUUID()}",
                [new OrderItemRequest("Pancakes", UUID.randomUUID(), 1)], 1)

        expect:
        client.post("/orders", order).status() == 201
        client.post("/orders", order).status() == 429
    }
}
