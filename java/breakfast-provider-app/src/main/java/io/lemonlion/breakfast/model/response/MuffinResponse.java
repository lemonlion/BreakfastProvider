package io.lemonlion.breakfast.model.response;

import java.time.Instant;
import java.util.List;
import java.util.UUID;

/** Twin of C# {@code MuffinResponse}. */
public record MuffinResponse(
        UUID batchId,
        List<String> ingredients,
        List<String> toppings,
        int bakingTemperature,
        int bakingDuration,
        Instant createdAt) {
}
