package io.lemonlion.breakfast.model.request;

import java.time.Instant;

/** Twin of C# {@code ReservationRequest}. */
public record ReservationRequest(
        String customerName, int tableNumber, int partySize, Instant reservedAt, String contactPhone) {
}
