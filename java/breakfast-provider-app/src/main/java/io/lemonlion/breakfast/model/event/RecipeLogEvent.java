package io.lemonlion.breakfast.model.event;

import java.time.Instant;
import java.util.List;
import java.util.UUID;

/** Twin of C# {@code RecipeLogEvent} (a Kafka event). */
public record RecipeLogEvent(
        UUID orderId, String recipeType, List<String> ingredients, List<String> toppings, Instant loggedAt) {
}
