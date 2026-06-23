package io.lemonlion.breakfast.model.response;

import java.util.UUID;

/** Twin of C# {@code OrderItemResponse}. */
public record OrderItemResponse(String itemType, UUID batchId, int quantity) {
}
