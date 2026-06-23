package io.lemonlion.breakfast.model.event;

import java.time.Instant;
import java.util.UUID;

/** Twin of C# {@code DailySpecialOrderedEvent} (a Pub/Sub event). */
public record DailySpecialOrderedEvent(
        UUID orderId, String specialName, String customerName, int remainingOrders, Instant orderedAt) {
}
