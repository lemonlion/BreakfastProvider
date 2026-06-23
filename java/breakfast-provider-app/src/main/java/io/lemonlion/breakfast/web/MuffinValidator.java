package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.config.ToppingRulesConfig;
import io.lemonlion.breakfast.model.request.BakingProfile;
import io.lemonlion.breakfast.model.request.MuffinRequest;
import io.lemonlion.breakfast.model.request.MuffinTopping;
import org.springframework.stereotype.Component;

/** Twin of C# {@code MuffinRequestValidator} (nested baking + structured toppings). */
@Component
public class MuffinValidator {

    private final ToppingRulesConfig toppingRules;

    public MuffinValidator(ToppingRulesConfig toppingRules) {
        this.toppingRules = toppingRules;
    }

    public void validate(MuffinRequest request) {
        ValidationException.Collector errors = new ValidationException.Collector();

        requireText(errors, "Milk", request.milk());
        requireText(errors, "Flour", request.flour());
        requireText(errors, "Eggs", request.eggs());
        requireText(errors, "Apples", request.apples());
        requireText(errors, "Cinnamon", request.cinnamon());

        BakingProfile baking = request.baking();
        if (baking == null) {
            errors.add("Baking", "'Baking' profile is required.");
        } else {
            if (baking.temperature() < 150 || baking.temperature() > 220) {
                errors.add("Baking.Temperature", "Baking temperature must be between 150 and 220 degrees.");
            }
            if (baking.durationMinutes() < 10 || baking.durationMinutes() > 60) {
                errors.add("Baking.DurationMinutes", "Baking duration must be between 10 and 60 minutes.");
            }
            if (baking.panType() == null || baking.panType().isBlank()) {
                errors.add("Baking.PanType", "'Pan Type' is required.");
            } else if (Xss.containsHtmlOrScript(baking.panType())) {
                errors.add("Baking.PanType", "'Pan Type' must not contain HTML or script content.");
            }
        }

        if (request.toppings() != null) {
            for (int i = 0; i < request.toppings().size(); i++) {
                MuffinTopping topping = request.toppings().get(i);
                String prefix = "Toppings[" + i + "].";
                if (topping.name() == null || topping.name().isBlank()) {
                    errors.add(prefix + "Name", "Topping 'Name' is required.");
                } else if (Xss.containsHtmlOrScript(topping.name())) {
                    errors.add(prefix + "Name", "'Topping Name' must not contain HTML or script content.");
                }
                if (topping.amount() == null || topping.amount().isBlank()) {
                    errors.add(prefix + "Amount", "Topping 'Amount' is required.");
                } else if (Xss.containsHtmlOrScript(topping.amount())) {
                    errors.add(prefix + "Amount", "'Topping Amount' must not contain HTML or script content.");
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
