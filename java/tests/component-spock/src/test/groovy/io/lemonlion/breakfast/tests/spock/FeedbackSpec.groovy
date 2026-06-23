package io.lemonlion.breakfast.tests.spock

import io.lemonlion.breakfast.BreakfastProviderApplication
import io.lemonlion.breakfast.model.request.FeedbackRequest
import io.lemonlion.breakfast.model.response.FeedbackResponse
import io.lemonlion.breakfast.testsupport.BackendsInitializer
import io.lemonlion.breakfast.testsupport.BreakfastBackends
import io.lemonlion.breakfast.testsupport.BreakfastTestClient
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.boot.test.web.server.LocalServerPort
import org.springframework.test.context.ContextConfiguration
import spock.lang.Specification

/** Feedback domain component spec (Spock) — Spanner persistence. */
@SpringBootTest(classes = BreakfastProviderApplication, webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer)
class FeedbackSpec extends Specification {

    @LocalServerPort
    int port

    BreakfastTestClient client

    def setup() {
        client = new BreakfastTestClient("http://127.0.0.1:${port}")
        BreakfastBackends.resetFakes()
    }

    def "feedback is created and retrievable by id"() {
        when:
        def created = client.post("/feedback", new FeedbackRequest("Alice", "order-${UUID.randomUUID()}", 5, "Loved it"))

        then:
        created.status() == 201
        def feedback = created.as(FeedbackResponse)
        feedback.feedbackId()
        client.get("/feedback/${feedback.feedbackId()}").status() == 200
    }

    def "a rating outside 1-5 is rejected"() {
        when:
        def response = client.post("/feedback", new FeedbackRequest("Alice", "order-1", 9, "Too high"))

        then:
        response.status() == 400
        response.bodyContains("'Rating' must be between 1 and 5.")
    }
}
