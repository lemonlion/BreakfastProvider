package io.lemonlion.breakfast.model.request;

/** Twin of C# {@code FeedbackRequest}. */
public record FeedbackRequest(String customerName, String orderId, int rating, String comment) {
}
