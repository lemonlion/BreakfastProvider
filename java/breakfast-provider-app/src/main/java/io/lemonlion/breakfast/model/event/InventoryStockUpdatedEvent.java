package io.lemonlion.breakfast.model.event;

import java.math.BigDecimal;
import java.time.Instant;

/** Twin of C# {@code InventoryStockUpdatedEvent} (a Pub/Sub event). */
public record InventoryStockUpdatedEvent(
        int itemId, String name, BigDecimal previousQuantity, BigDecimal newQuantity, Instant updatedAt) {
}
