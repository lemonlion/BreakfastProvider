package io.lemonlion.breakfast.model.response;

import java.time.Instant;
import java.util.List;
import java.util.UUID;

/** Twin of C# {@code OrderResponse}. */
public record OrderResponse(
        UUID orderId,
        String customerName,
        List<OrderItemResponse> items,
        Integer tableNumber,
        String status,
        Instant createdAt) {
}
