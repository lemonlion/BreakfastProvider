package io.lemonlion.breakfast.model.event;

import java.math.BigDecimal;
import java.time.Instant;
import java.util.List;
import java.util.UUID;

/** Twin of C# {@code RecipeCostCalculatedEvent} (consumed from Kafka). */
public record RecipeCostCalculatedEvent(
        UUID calculationId,
        String recipeName,
        List<String> ingredients,
        BigDecimal totalCost,
        String currency,
        Instant calculatedAt) {
}
