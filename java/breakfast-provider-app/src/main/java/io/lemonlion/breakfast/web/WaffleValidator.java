package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.config.ToppingRulesConfig;
import io.lemonlion.breakfast.model.request.WaffleRequest;
import org.springframework.stereotype.Component;

/** Twin of C# {@code WaffleRequestValidator}. */
@Component
public class WaffleValidator {

    private final ToppingRulesConfig toppingRules;

    public WaffleValidator(ToppingRulesConfig toppingRules) {
        this.toppingRules = toppingRules;
    }

    public void validate(WaffleRequest request) {
        ValidationException.Collector errors = new ValidationException.Collector();

        requireText(errors, "Milk", request.milk());
        requireText(errors, "Flour", request.flour());
        requireText(errors, "Eggs", request.eggs());
        requireText(errors, "Butter", request.butter());

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
