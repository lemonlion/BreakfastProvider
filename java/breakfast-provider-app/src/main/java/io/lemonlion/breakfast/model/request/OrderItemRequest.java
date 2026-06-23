package io.lemonlion.breakfast.model.request;

import java.util.UUID;

/**
 * Twin of C# {@code OrderItemRequest}. {@code quantity} is nullable so an omitted value defaults to 1
 * (C# behaviour) while an explicit {@code 0} is preserved and rejected by validation.
 */
public record OrderItemRequest(String itemType, UUID batchId, Integer quantity) {

    /** Effective quantity: 1 when the client omitted it (null), otherwise the supplied value. */
    public int effectiveQuantity() {
        return quantity == null ? 1 : quantity;
    }
}
