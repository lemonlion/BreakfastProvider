package io.lemonlion.breakfast.tests.spock

import io.lemonlion.breakfast.BreakfastProviderApplication
import io.lemonlion.breakfast.model.request.RecipeReviewRequest
import io.lemonlion.breakfast.model.response.RecipeReviewResponse
import io.lemonlion.breakfast.testsupport.BackendsInitializer
import io.lemonlion.breakfast.testsupport.BreakfastBackends
import io.lemonlion.breakfast.testsupport.BreakfastTestClient
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.boot.test.web.server.LocalServerPort
import org.springframework.test.context.ContextConfiguration
import spock.lang.Specification

/** RecipeReviews domain component spec (Spock) — MongoDB persistence. */
@SpringBootTest(classes = BreakfastProviderApplication, webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer)
class RecipeReviewsSpec extends Specification {

    @LocalServerPort
    int port

    BreakfastTestClient client

    def setup() {
        client = new BreakfastTestClient("http://127.0.0.1:${port}")
        BreakfastBackends.resetFakes()
    }

    def "a review is created and retrievable by id"() {
        when:
        def created = client.post("/recipe-reviews",
                new RecipeReviewRequest("Classic Pancakes", "Alice", 5, "Delicious", ["fluffy", "sweet"]))

        then:
        created.status() == 201
        def review = created.as(RecipeReviewResponse)
        review.reviewId()
        review.tags().contains("fluffy")
        client.get("/recipe-reviews/${review.reviewId()}").status() == 200
    }

    def "a rating outside 1-5 is rejected"() {
        when:
        def response = client.post("/recipe-reviews",
                new RecipeReviewRequest("Classic Pancakes", "Alice", 7, "x", []))

        then:
        response.status() == 400
        response.bodyContains("'Rating' must be between 1 and 5.")
    }
}
