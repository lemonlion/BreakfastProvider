package io.lemonlion.breakfast.model.response;

import java.time.Instant;

/** Twin of C# {@code CustomerPreferenceResponse}. */
public record CustomerPreferenceResponse(
        String customerId,
        String customerName,
        String preferredMilkType,
        boolean likesExtraToppings,
        String favouriteItem,
        Instant updatedAt) {
}
