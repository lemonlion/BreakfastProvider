package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.model.request.InventoryItemRequest;
import java.math.BigDecimal;
import org.springframework.stereotype.Component;

/** Twin of C# {@code InventoryItemRequestValidator}. */
@Component
public class InventoryValidator {

    public void validate(InventoryItemRequest request) {
        ValidationException.Collector errors = new ValidationException.Collector();

        requireText(errors, "Name", request.name());
        requireText(errors, "Category", request.category());
        requireText(errors, "Unit", request.unit());

        if (request.quantity().compareTo(BigDecimal.ZERO) < 0) {
            errors.add("Quantity", "'Quantity' must be greater than or equal to zero.");
        }
        if (request.reorderLevel().compareTo(BigDecimal.ZERO) < 0) {
            errors.add("ReorderLevel", "'Reorder Level' must be greater than or equal to zero.");
        }

        if (errors.hasErrors()) {
            throw new ValidationException(errors.build());
        }
    }

    private static void requireText(ValidationException.Collector errors, String field, String value) {
        if (value == null || value.isBlank()) {
            errors.add(field, "'" + field + "' is required.");
        } else if (Xss.containsHtmlOrScript(value)) {
            errors.add(field, "'" + field + "' must not contain HTML or script content.");
        }
    }
}
