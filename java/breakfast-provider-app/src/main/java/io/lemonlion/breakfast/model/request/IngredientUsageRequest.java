package io.lemonlion.breakfast.model.request;

import java.math.BigDecimal;

/** Twin of C# {@code IngredientUsageRequest}. */
public record IngredientUsageRequest(String ingredientName, BigDecimal quantityUsed, String unit, String recipeName) {

    public IngredientUsageRequest {
        if (quantityUsed == null) {
            quantityUsed = BigDecimal.ZERO;
        }
    }
}
