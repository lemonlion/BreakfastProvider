package io.lemonlion.breakfast.model.request;

/** Twin of C# {@code ChefNoteRequest}. */
public record ChefNoteRequest(String recipeName, String chefName, String noteText, String category) {
}
