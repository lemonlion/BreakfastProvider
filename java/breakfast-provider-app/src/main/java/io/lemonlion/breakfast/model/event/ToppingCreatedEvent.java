package io.lemonlion.breakfast.model.event;

import java.time.Instant;
import java.util.UUID;

/** Twin of C# {@code ToppingCreatedEvent} (a Pub/Sub event). */
public record ToppingCreatedEvent(
        UUID toppingId, String name, String category, boolean isSeasonal, Instant createdAt) {
}
