package io.lemonlion.breakfast.model.response;

import java.time.Instant;
import java.util.List;
import java.util.UUID;

/** Twin of C# {@code PancakeResponse}. */
public record PancakeResponse(UUID batchId, List<String> ingredients, List<String> toppings, Instant createdAt) {
}
