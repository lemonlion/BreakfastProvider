package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.core.type.TypeReference;
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

    @Test
    @DisplayName("submitting a review returns the created review")
    void submitReturnsCreated() {
        TestResponse created = client.post("/recipe-reviews", valid());
        assertThat(created.status()).isEqualTo(201);
        RecipeReviewResponse review = created.as(RecipeReviewResponse.class);
        assertThat(review.reviewId()).isNotBlank();
        assertThat(review.rating()).isEqualTo(5);
    }

    @Test
    @DisplayName("reviews are listed by recipe")
    void listByRecipe() {
        String recipe = "Recipe-" + UUID.randomUUID();
        RecipeReviewResponse created = client.post("/recipe-reviews",
                new RecipeReviewRequest(recipe, "Alice", 5, "Delicious", List.of("fluffy"))).as(RecipeReviewResponse.class);

        TestResponse list = client.get("/recipe-reviews/recipe/" + recipe);
        assertThat(list.status()).isEqualTo(200);
        assertThat(list.as(new TypeReference<List<RecipeReviewResponse>>() { }))
                .anyMatch(r -> r.reviewId().equals(created.reviewId()));
    }

    @Test
    @DisplayName("a missing recipe name is rejected")
    void rejectsMissingRecipeName() {
        TestResponse response = client.post("/recipe-reviews",
                new RecipeReviewRequest(null, "Alice", 5, "Delicious", List.of()));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Recipe Name' must not be empty.")).isTrue();
    }
}
