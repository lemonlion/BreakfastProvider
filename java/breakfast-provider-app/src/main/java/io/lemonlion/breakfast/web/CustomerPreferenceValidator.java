package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.model.request.CustomerPreferenceRequest;
import org.springframework.stereotype.Component;

/** Twin of C# {@code CustomerPreferenceRequestValidator}. */
@Component
public class CustomerPreferenceValidator {

    public void validate(CustomerPreferenceRequest request) {
        ValidationException.Collector errors = new ValidationException.Collector();
        notEmptyMax(errors, "Customer Id", request.customerId(), 100);
        notEmptyMax(errors, "Customer Name", request.customerName(), 200);
        max(errors, "Preferred Milk Type", request.preferredMilkType(), 50);
        max(errors, "Favourite Item", request.favouriteItem(), 100);
        if (errors.hasErrors()) {
            throw new ValidationException(errors.build());
        }
    }

    private static void notEmptyMax(ValidationException.Collector errors, String field, String value, int maxLen) {
        if (value == null || value.isBlank()) {
            errors.add(field, "'" + field + "' must not be empty.");
        } else if (value.length() > maxLen) {
            errors.add(field, "'" + field + "' must be " + maxLen + " characters or fewer.");
        }
    }

    private static void max(ValidationException.Collector errors, String field, String value, int maxLen) {
        if (value != null && value.length() > maxLen) {
            errors.add(field, "'" + field + "' must be " + maxLen + " characters or fewer.");
        }
    }
}
