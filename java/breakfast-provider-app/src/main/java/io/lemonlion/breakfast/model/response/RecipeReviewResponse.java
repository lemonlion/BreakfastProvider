package io.lemonlion.breakfast.model.response;

import java.time.Instant;
import java.util.List;

/** Twin of C# {@code RecipeReviewResponse}. */
public record RecipeReviewResponse(
        String reviewId,
        String recipeName,
        String reviewerName,
        int rating,
        String comments,
        List<String> tags,
        Instant createdAt) {
}
