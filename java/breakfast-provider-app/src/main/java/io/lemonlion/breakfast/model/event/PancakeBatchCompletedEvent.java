package io.lemonlion.breakfast.model.event;

import java.time.Instant;
import java.util.List;
import java.util.UUID;

/** Twin of C# {@code PancakeBatchCompletedEvent} (a Pub/Sub event). */
public record PancakeBatchCompletedEvent(
        UUID batchId, List<String> ingredients, List<String> toppings, Instant completedAt) {
}
