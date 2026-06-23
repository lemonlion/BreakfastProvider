package io.lemonlion.breakfast.model.event;

import java.time.Instant;

/** Twin of C# {@code MenuAvailabilityChangedEvent} (a Pub/Sub event). */
public record MenuAvailabilityChangedEvent(String itemName, boolean isAvailable, String reason, Instant changedAt) {
}
