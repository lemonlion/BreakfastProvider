package io.lemonlion.breakfast.service;

import io.lemonlion.breakfast.model.request.RecipeReviewRequest;
import io.lemonlion.breakfast.model.response.RecipeReviewResponse;
import java.util.List;
import java.util.Optional;

/** Twin of C# {@code IRecipeReviewService} (MongoDB-backed). */
public interface RecipeReviewService {

    RecipeReviewResponse create(RecipeReviewRequest request);

    Optional<RecipeReviewResponse> getById(String reviewId);

    List<RecipeReviewResponse> listByRecipe(String recipeName);
}
