package io.lemonlion.breakfast.model.response;

import java.time.Instant;

/** Twin of C# {@code FeedbackResponse}. */
public record FeedbackResponse(
        String feedbackId, String customerName, String orderId, int rating, String comment, Instant createdAt) {
}
