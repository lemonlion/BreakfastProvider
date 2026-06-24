package io.lemonlion.breakfast.reporting;

import java.time.Instant;
import java.util.UUID;

/** Pub/Sub wire payload for a completed recipe batch (twin of the C# batch-completion event). */
public record BatchCompletionMessage(String recipeType, UUID batchId, Instant completedAt) {
}
