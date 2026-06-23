package io.lemonlion.breakfast.model.event;

import java.time.Instant;
import java.util.List;
import java.util.UUID;

/** Twin of C# {@code MuffinBatchCompletedEvent} (a Pub/Sub event). */
public record MuffinBatchCompletedEvent(
        UUID batchId, List<String> ingredients, List<String> toppings, int bakingTemperature, Instant completedAt) {
}
