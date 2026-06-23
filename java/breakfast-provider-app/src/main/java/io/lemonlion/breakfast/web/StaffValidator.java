package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.model.request.StaffMemberRequest;
import java.util.List;
import java.util.regex.Pattern;
import org.springframework.stereotype.Component;

/** Twin of C# {@code StaffMemberRequestValidator}. */
@Component
public class StaffValidator {

    private static final List<String> VALID_ROLES = List.of(
            "Chef", "Sous Chef", "Line Cook", "Prep Cook", "Server", "Host", "Manager", "Dishwasher");
    private static final Pattern EMAIL = Pattern.compile("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$");

    public void validate(StaffMemberRequest request) {
        ValidationException.Collector errors = new ValidationException.Collector();

        if (isBlank(request.name())) {
            errors.add("Name", "'Name' is required.");
        } else if (Xss.containsHtmlOrScript(request.name())) {
            errors.add("Name", "'Name' must not contain HTML or script content.");
        }

        if (isBlank(request.role())) {
            errors.add("Role", "'Role' is required.");
        } else if (VALID_ROLES.stream().noneMatch(r -> r.equalsIgnoreCase(request.role()))) {
            errors.add("Role", "'Role' must be one of: " + String.join(", ", VALID_ROLES) + ".");
        }

        if (isBlank(request.email())) {
            errors.add("Email", "'Email' is required.");
        } else {
            if (!EMAIL.matcher(request.email()).matches()) {
                errors.add("Email", "'Email' must be a valid email address.");
            }
            if (Xss.containsHtmlOrScript(request.email())) {
                errors.add("Email", "'Email' must not contain HTML or script content.");
            }
        }

        if (errors.hasErrors()) {
            throw new ValidationException(errors.build());
        }
    }

    private static boolean isBlank(String value) {
        return value == null || value.isBlank();
    }
}
