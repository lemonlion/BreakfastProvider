package io.lemonlion.breakfast.model.event;

import java.math.BigDecimal;
import java.time.Instant;

/** Twin of C# {@code InventoryItemAddedEvent} (a Pub/Sub event). */
public record InventoryItemAddedEvent(
        int itemId, String name, String category, BigDecimal quantity, String unit, Instant addedAt) {
}
