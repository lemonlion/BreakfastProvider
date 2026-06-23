package io.lemonlion.breakfast.model.response;

import java.util.UUID;

/** Twin of C# {@code DailySpecialOrderResponse}. */
public record DailySpecialOrderResponse(
        UUID orderConfirmationId, UUID specialId, int quantityOrdered, int remainingQuantity) {
}
