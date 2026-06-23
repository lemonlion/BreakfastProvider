package io.lemonlion.breakfast.model.response;

import java.math.BigDecimal;
import java.time.Instant;

/** Twin of C# {@code InventoryItemResponse}. */
public record InventoryItemResponse(
        int id,
        String name,
        String category,
        BigDecimal quantity,
        String unit,
        BigDecimal reorderLevel,
        Instant lastRestockedAt,
        Instant createdAt) {
}
