package io.lemonlion.breakfast.model.response;

import java.math.BigDecimal;
import java.time.Instant;

/** Twin of C# {@code IngredientWasteResponse}. */
public record IngredientWasteResponse(
        String wasteId, String ingredientName, BigDecimal quantityWasted, String unit, String recipeName,
        String reason, Instant recordedAt) {
}
