package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.model.request.ChefNoteRequest;
import io.lemonlion.breakfast.model.request.UpdateChefNoteRequest;
import org.springframework.stereotype.Component;

/** Twin of C# {@code ChefNoteRequestValidator} / {@code UpdateChefNoteRequestValidator}. */
@Component
public class ChefNoteValidator {

    public void validate(ChefNoteRequest request) {
        ValidationException.Collector errors = new ValidationException.Collector();
        notEmptyMax(errors, "Recipe Name", request.recipeName(), 200);
        notEmptyMax(errors, "Chef Name", request.chefName(), 100);
        notEmptyMax(errors, "Note Text", request.noteText(), 2000);
        notEmptyMax(errors, "Category", request.category(), 100);
        throwIfAny(errors);
    }

    public void validate(UpdateChefNoteRequest request) {
        ValidationException.Collector errors = new ValidationException.Collector();
        notEmptyMax(errors, "Note Text", request.noteText(), 2000);
        if (request.category() != null && request.category().length() > 100) {
            errors.add("Category", "'Category' must be 100 characters or fewer.");
        }
        throwIfAny(errors);
    }

    private static void notEmptyMax(ValidationException.Collector errors, String field, String value, int max) {
        if (value == null || value.isBlank()) {
            errors.add(field, "'" + field + "' must not be empty.");
        } else if (value.length() > max) {
            errors.add(field, "'" + field + "' must be " + max + " characters or fewer.");
        }
    }

    private static void throwIfAny(ValidationException.Collector errors) {
        if (errors.hasErrors()) {
            throw new ValidationException(errors.build());
        }
    }
}
