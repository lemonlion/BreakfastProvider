package io.lemonlion.breakfast.model.response;

import java.util.List;

/** Twin of C# {@code MenuItemResponse}. */
public record MenuItemResponse(String name, String description, boolean isAvailable, List<String> requiredIngredients) {
}
