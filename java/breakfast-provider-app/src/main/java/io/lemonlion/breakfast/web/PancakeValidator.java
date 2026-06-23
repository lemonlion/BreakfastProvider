package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.config.ToppingRulesConfig;
import io.lemonlion.breakfast.model.request.PancakeRequest;
import org.springframework.stereotype.Component;

/** Twin of C# {@code PancakeRequestValidator}. */
@Component
public class PancakeValidator {

    private final ToppingRulesConfig toppingRules;

    public PancakeValidator(ToppingRulesConfig toppingRules) {
        this.toppingRules = toppingRules;
    }

    public void validate(PancakeRequest request) {
        ValidationException.Collector errors = new ValidationException.Collector();

        requireText(errors, "Milk", request.milk());
        requireText(errors, "Flour", request.flour());
        requireText(errors, "Eggs", request.eggs());

        if (request.toppings() != null) {
            for (String topping : request.toppings()) {
                if (Xss.containsHtmlOrScript(topping)) {
                    errors.add("Toppings", "'Toppings' must not contain HTML or script content.");
                    break;
                }
            }
            if (request.toppings().size() > toppingRules.getMaxToppingsPerItem()) {
                errors.add("Toppings",
                        "Maximum toppings exceeded. Limit is " + toppingRules.getMaxToppingsPerItem() + ".");
            }
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
