package io.lemonlion.breakfast.model.response;

import java.math.BigDecimal;

/** Twin of C# {@code IngredientUsageSummaryResponse}. */
public record IngredientUsageSummaryResponse(
        String ingredientName, BigDecimal totalQuantityUsed, String unit, int recordCount) {
}
