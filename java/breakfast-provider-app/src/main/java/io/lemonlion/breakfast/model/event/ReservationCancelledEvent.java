package io.lemonlion.breakfast.model.event;

import java.time.Instant;

/** Twin of C# {@code ReservationCancelledEvent} (a Pub/Sub event). */
public record ReservationCancelledEvent(int reservationId, String customerName, String reason, Instant cancelledAt) {
}
