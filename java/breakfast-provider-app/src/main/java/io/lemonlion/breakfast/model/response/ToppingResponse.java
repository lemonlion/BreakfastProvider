package io.lemonlion.breakfast.model.response;

import java.util.UUID;

/** Twin of C# {@code ToppingResponse}. */
public record ToppingResponse(UUID toppingId, String name, String category) {
}
