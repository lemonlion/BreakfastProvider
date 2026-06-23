package io.lemonlion.breakfast.model.response;

import java.time.Instant;

/** Twin of C# {@code ChefNoteResponse}. */
public record ChefNoteResponse(
        String noteId,
        String recipeName,
        String chefName,
        String noteText,
        String category,
        Instant createdAt,
        Instant updatedAt) {
}
