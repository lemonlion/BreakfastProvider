package io.lemonlion.breakfast.model.response;

import java.time.Instant;

/** Twin of C# {@code StaffMemberResponse}. */
public record StaffMemberResponse(
        int id, String name, String role, String email, boolean isActive, Instant hiredAt, Instant createdAt) {
}
