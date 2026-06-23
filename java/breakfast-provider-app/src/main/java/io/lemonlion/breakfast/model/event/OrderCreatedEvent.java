package io.lemonlion.breakfast.model.event;

import java.time.Instant;
import java.util.UUID;

/** Twin of C# {@code OrderCreatedEvent} — published via the outbox to EventGrid when an order is created. */
public record OrderCreatedEvent(
        UUID orderId,
        String customerName,
        int itemCount,
        Integer tableNumber,
        Instant createdAt) {
}
