package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.model.request.IngredientUsageRequest;
import java.math.BigDecimal;
import org.springframework.stereotype.Component;

/** Twin of C# {@code IngredientUsageRequestValidator}. */
@Component
public class IngredientUsageValidator {

    public void validate(IngredientUsageRequest request) {
        ValidationException.Collector errors = new ValidationException.Collector();
        notEmptyMax(errors, "Ingredient Name", request.ingredientName(), 200);
        if (request.quantityUsed().compareTo(BigDecimal.ZERO) <= 0) {
            errors.add("QuantityUsed", "'Quantity Used' must be greater than zero.");
        }
        notEmptyMax(errors, "Unit", request.unit(), 50);
        notEmptyMax(errors, "Recipe Name", request.recipeName(), 200);
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
