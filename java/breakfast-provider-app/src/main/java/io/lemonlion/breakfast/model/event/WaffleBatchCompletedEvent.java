package io.lemonlion.breakfast.model.event;

import java.time.Instant;
import java.util.List;
import java.util.UUID;

/** Twin of C# {@code WaffleBatchCompletedEvent} (a Pub/Sub event). */
public record WaffleBatchCompletedEvent(
        UUID batchId, List<String> ingredients, List<String> toppings, Instant completedAt) {
}
