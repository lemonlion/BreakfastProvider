package io.lemonlion.breakfast.model.event;

import java.time.Instant;

/** Twin of C# {@code ReservationConfirmedEvent} (a Pub/Sub event). */
public record ReservationConfirmedEvent(
        int reservationId, String customerName, int partySize, Instant reservedAt, Instant confirmedAt) {
}
