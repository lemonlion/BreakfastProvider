package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.model.request.ToppingRequest;
import io.lemonlion.breakfast.model.request.UpdateToppingRequest;
import org.springframework.stereotype.Component;

/** Twin of C# {@code ToppingRequestValidator} / {@code UpdateToppingRequestValidator} (identical rules). */
@Component
public class ToppingValidator {

    public void validate(ToppingRequest request) {
        validateNameCategory(request.name(), request.category());
    }

    public void validate(UpdateToppingRequest request) {
        validateNameCategory(request.name(), request.category());
    }

    private void validateNameCategory(String name, String category) {
        ValidationException.Collector errors = new ValidationException.Collector();
        checkField(errors, "Name", name);
        checkField(errors, "Category", category);
        if (errors.hasErrors()) {
            throw new ValidationException(errors.build());
        }
    }

    private static void checkField(ValidationException.Collector errors, String field, String value) {
        if (value == null || value.isBlank()) {
            errors.add(field, "'" + field + "' is required.");
            return;
        }
        if (value.length() > 100) {
            errors.add(field, "'" + field + "' must not exceed 100 characters.");
        }
        if (Xss.containsHtmlOrScript(value)) {
            errors.add(field, "'" + field + "' must not contain HTML or script content.");
        }
    }
}
