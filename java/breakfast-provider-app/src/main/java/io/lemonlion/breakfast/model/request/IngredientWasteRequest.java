package io.lemonlion.breakfast.model.request;

import java.math.BigDecimal;

/** Twin of C# {@code IngredientWasteRequest}. */
public record IngredientWasteRequest(
        String ingredientName, BigDecimal quantityWasted, String unit, String recipeName, String reason) {

    public IngredientWasteRequest {
        if (quantityWasted == null) {
            quantityWasted = BigDecimal.ZERO;
        }
    }
}
