package io.lemonlion.breakfast.tests.testng;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.core.type.TypeReference;
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

    @Test
    public void submitReturnsCreated() {
        TestResponse created = client.post("/recipe-reviews", valid());
        assertThat(created.status()).isEqualTo(201);
        RecipeReviewResponse review = created.as(RecipeReviewResponse.class);
        assertThat(review.reviewId()).isNotBlank();
        assertThat(review.rating()).isEqualTo(5);
    }

    @Test
    public void listByRecipe() {
        String recipe = "Recipe-" + UUID.randomUUID();
        RecipeReviewResponse created = client.post("/recipe-reviews",
                new RecipeReviewRequest(recipe, "Alice", 5, "Delicious", List.of("fluffy"))).as(RecipeReviewResponse.class);
        TestResponse list = client.get("/recipe-reviews/recipe/" + recipe);
        assertThat(list.status()).isEqualTo(200);
        assertThat(list.as(new TypeReference<List<RecipeReviewResponse>>() { }))
                .anyMatch(r -> r.reviewId().equals(created.reviewId()));
    }

    @Test
    public void rejectsMissingRecipeName() {
        TestResponse response = client.post("/recipe-reviews",
                new RecipeReviewRequest(null, "Alice", 5, "Delicious", List.of()));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Recipe Name' must not be empty.")).isTrue();
    }
}
