package io.lemonlion.breakfast.model.response;

import java.math.BigDecimal;
import java.time.Instant;

/** Twin of C# {@code IngredientUsageResponse}. */
public record IngredientUsageResponse(
        String usageId, String ingredientName, BigDecimal quantityUsed, String unit, String recipeName,
        Instant recordedAt) {
}
