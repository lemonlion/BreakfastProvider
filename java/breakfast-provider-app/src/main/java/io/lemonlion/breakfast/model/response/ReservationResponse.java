package io.lemonlion.breakfast.model.response;

import java.time.Instant;

/** Twin of C# {@code ReservationResponse}. */
public record ReservationResponse(
        int id,
        String customerName,
        int tableNumber,
        int partySize,
        Instant reservedAt,
        String status,
        String contactPhone,
        Instant createdAt) {
}
