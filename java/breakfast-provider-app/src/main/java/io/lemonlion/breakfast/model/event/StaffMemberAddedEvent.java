package io.lemonlion.breakfast.model.event;

import java.time.Instant;

/** Twin of C# {@code StaffMemberAddedEvent} (a Pub/Sub event). */
public record StaffMemberAddedEvent(int staffId, String name, String role, Instant addedAt) {
}
