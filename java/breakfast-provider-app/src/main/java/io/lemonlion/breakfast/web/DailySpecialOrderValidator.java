package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.config.DailySpecialsConfig;
import io.lemonlion.breakfast.model.request.DailySpecialOrderRequest;
import org.springframework.stereotype.Component;

/** Twin of C# {@code DailySpecialOrderRequestValidator}. */
@Component
public class DailySpecialOrderValidator {

    private final DailySpecialsConfig config;

    public DailySpecialOrderValidator(DailySpecialsConfig config) {
        this.config = config;
    }

    public void validate(DailySpecialOrderRequest request) {
        ValidationException.Collector errors = new ValidationException.Collector();

        if (request.specialId() == null) {
            errors.add("SpecialId", "'Special Id' is required.");
        }
        if (request.quantity() <= 0) {
            errors.add("Quantity", "Quantity must be greater than zero.");
        } else if (request.quantity() > config.getMaxOrdersPerSpecial()) {
            errors.add("Quantity", "Quantity must not exceed " + config.getMaxOrdersPerSpecial() + ".");
        }

        if (errors.hasErrors()) {
            throw new ValidationException(errors.build());
        }
    }
}
