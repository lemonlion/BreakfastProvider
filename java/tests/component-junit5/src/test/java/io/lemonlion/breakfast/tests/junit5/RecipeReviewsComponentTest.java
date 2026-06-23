package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;

import io.lemonlion.breakfast.model.request.RecipeReviewRequest;
import io.lemonlion.breakfast.model.response.RecipeReviewResponse;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.List;
import java.util.UUID;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

/** RecipeReviews domain component tests (JUnit 5) — MongoDB persistence. */
@DisplayName("RecipeReviews")
class RecipeReviewsComponentTest extends ComponentTestBase {

    private static RecipeReviewRequest valid() {
        return new RecipeReviewRequest("Classic Pancakes", "Alice", 5, "Delicious", List.of("fluffy", "sweet"));
    }

    @Test
    @DisplayName("a review is created and retrievable by id")
    void createAndRetrieve() {
        TestResponse created = client.post("/recipe-reviews", valid());
        assertThat(created.status()).isEqualTo(201);
        RecipeReviewResponse review = created.as(RecipeReviewResponse.class);
        assertThat(review.reviewId()).isNotBlank();
        assertThat(review.tags()).contains("fluffy");

        TestResponse fetched = client.get("/recipe-reviews/" + review.reviewId());
        assertThat(fetched.status()).isEqualTo(200);
        assertThat(fetched.as(RecipeReviewResponse.class).rating()).isEqualTo(5);
    }

    @Test
    @DisplayName("a rating outside 1-5 is rejected")
    void rejectsBadRating() {
        TestResponse response = client.post("/recipe-reviews",
                new RecipeReviewRequest("Classic Pancakes", "Alice", 7, "x", List.of()));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Rating' must be between 1 and 5.")).isTrue();
    }

    @Test
    @DisplayName("retrieving an unknown review returns 404")
    void getMissing() {
        assertThat(client.get("/recipe-reviews/unknown-" + UUID.randomUUID()).status()).isEqualTo(404);
    }
}
