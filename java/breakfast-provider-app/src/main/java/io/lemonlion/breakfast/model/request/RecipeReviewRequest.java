package io.lemonlion.breakfast.model.request;

import java.util.List;

/** Twin of C# {@code RecipeReviewRequest}. */
public record RecipeReviewRequest(
        String recipeName, String reviewerName, int rating, String comments, List<String> tags) {
}
