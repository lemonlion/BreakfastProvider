package io.lemonlion.breakfast.model.request;

import java.util.UUID;

/** Twin of C# {@code DailySpecialOrderRequest}. */
public record DailySpecialOrderRequest(UUID specialId, int quantity) {
}
