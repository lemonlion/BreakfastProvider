package io.lemonlion.breakfast.tests.testng;

import static org.assertj.core.api.Assertions.assertThat;

import io.lemonlion.breakfast.model.request.RecipeReviewRequest;
import io.lemonlion.breakfast.model.response.RecipeReviewResponse;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.List;
import java.util.UUID;
import org.testng.annotations.Test;

/** RecipeReviews domain component tests (TestNG). */
public class RecipeReviewsTestNgTest extends ComponentTestBaseNg {

    private static RecipeReviewRequest valid() {
        return new RecipeReviewRequest("Classic Pancakes", "Alice", 5, "Delicious", List.of("fluffy"));
    }

    @Test
    public void createAndRetrieve() {
        TestResponse created = client.post("/recipe-reviews", valid());
        assertThat(created.status()).isEqualTo(201);
        RecipeReviewResponse review = created.as(RecipeReviewResponse.class);
        assertThat(review.reviewId()).isNotBlank();
        assertThat(client.get("/recipe-reviews/" + review.reviewId()).status()).isEqualTo(200);
    }

    @Test
    public void rejectsBadRating() {
        TestResponse response = client.post("/recipe-reviews",
                new RecipeReviewRequest("Classic Pancakes", "Alice", 7, "x", List.of()));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Rating' must be between 1 and 5.")).isTrue();
    }

    @Test
    public void getMissing() {
        assertThat(client.get("/recipe-reviews/unknown-" + UUID.randomUUID()).status()).isEqualTo(404);
    }
}
