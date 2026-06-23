package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.model.request.RecipeReviewRequest;
import org.springframework.stereotype.Component;

/** Twin of C# {@code RecipeReviewRequestValidator}. */
@Component
public class RecipeReviewValidator {

    public void validate(RecipeReviewRequest request) {
        ValidationException.Collector errors = new ValidationException.Collector();

        notEmptyMax(errors, "Recipe Name", request.recipeName(), 200);
        notEmptyMax(errors, "Reviewer Name", request.reviewerName(), 100);
        if (request.rating() < 1 || request.rating() > 5) {
            errors.add("Rating", "'Rating' must be between 1 and 5.");
        }
        if (request.comments() != null && request.comments().length() > 1000) {
            errors.add("Comments", "'Comments' must be 1000 characters or fewer.");
        }
        if (request.tags() != null && request.tags().size() > 10) {
            errors.add("Tags", "A maximum of 10 tags is allowed.");
        }

        if (errors.hasErrors()) {
            throw new ValidationException(errors.build());
        }
    }

    private static void notEmptyMax(ValidationException.Collector errors, String field, String value, int max) {
        if (value == null || value.isBlank()) {
            errors.add(field, "'" + field + "' must not be empty.");
        } else if (value.length() > max) {
            errors.add(field, "'" + field + "' must be " + max + " characters or fewer.");
        }
    }
}
