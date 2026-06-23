package io.lemonlion.breakfast.model.event;

import java.time.Instant;
import java.util.UUID;

/** Twin of C# {@code CustomerFeedbackReceivedEvent} (consumed from Pub/Sub). */
public record CustomerFeedbackReceivedEvent(
        UUID feedbackId, String customerName, String recipeName, int rating, String comments, Instant receivedAt) {
}
