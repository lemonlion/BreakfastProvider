package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.model.request.ReservationRequest;
import java.time.Instant;
import java.time.temporal.ChronoUnit;
import org.springframework.stereotype.Component;

/** Twin of C# {@code ReservationRequestValidator}. */
@Component
public class ReservationValidator {

    public void validate(ReservationRequest request) {
        ValidationException.Collector errors = new ValidationException.Collector();

        if (request.customerName() == null || request.customerName().isBlank()) {
            errors.add("CustomerName", "'Customer Name' is required.");
        } else if (Xss.containsHtmlOrScript(request.customerName())) {
            errors.add("CustomerName", "'Customer Name' must not contain HTML or script content.");
        }

        if (request.tableNumber() < 1 || request.tableNumber() > 50) {
            errors.add("TableNumber", "'Table Number' must be between 1 and 50.");
        }
        if (request.partySize() < 1 || request.partySize() > 20) {
            errors.add("PartySize", "'Party Size' must be between 1 and 20.");
        }
        if (request.reservedAt() == null
                || !request.reservedAt().isAfter(Instant.now().minus(5, ChronoUnit.MINUTES))) {
            errors.add("ReservedAt", "'Reserved At' must be in the future.");
        }
        if (request.contactPhone() != null && Xss.containsHtmlOrScript(request.contactPhone())) {
            errors.add("ContactPhone", "'Contact Phone' must not contain HTML or script content.");
        }

        if (errors.hasErrors()) {
            throw new ValidationException(errors.build());
        }
    }
}
