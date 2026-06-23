package io.lemonlion.breakfast.model.response;

import java.util.UUID;

/** Twin of C# {@code DailySpecialResponse}. */
public record DailySpecialResponse(UUID specialId, String name, String description, int remainingQuantity) {
}
