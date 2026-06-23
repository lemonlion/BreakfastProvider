package io.lemonlion.breakfast.model.request;

import java.math.BigDecimal;

/** Twin of C# {@code InventoryItemRequest} ({@code decimal} -> {@link BigDecimal}; omitted numbers default to 0). */
public record InventoryItemRequest(
        String name, String category, BigDecimal quantity, String unit, BigDecimal reorderLevel) {

    public InventoryItemRequest {
        if (quantity == null) {
            quantity = BigDecimal.ZERO;
        }
        if (reorderLevel == null) {
            reorderLevel = BigDecimal.ZERO;
        }
    }
}
