package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.model.request.FeedbackRequest;
import org.springframework.stereotype.Component;

/** Twin of C# {@code FeedbackRequestValidator}. */
@Component
public class FeedbackValidator {

    public void validate(FeedbackRequest request) {
        ValidationException.Collector errors = new ValidationException.Collector();

        if (request.customerName() == null || request.customerName().isBlank()) {
            errors.add("CustomerName", "'Customer Name' must not be empty.");
        } else if (request.customerName().length() > 200) {
            errors.add("CustomerName", "'Customer Name' must be 200 characters or fewer.");
        }
        if (request.orderId() == null || request.orderId().isBlank()) {
            errors.add("OrderId", "'Order Id' must not be empty.");
        }
        if (request.rating() < 1 || request.rating() > 5) {
            errors.add("Rating", "'Rating' must be between 1 and 5.");
        }
        if (request.comment() != null && request.comment().length() > 1000) {
            errors.add("Comment", "'Comment' must be 1000 characters or fewer.");
        }

        if (errors.hasErrors()) {
            throw new ValidationException(errors.build());
        }
    }
}
