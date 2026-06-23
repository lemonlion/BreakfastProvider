package io.lemonlion.breakfast.model.request;

import java.time.Instant;

/** Twin of C# {@code StaffMemberRequest}. {@code isActive} defaults to true when omitted. */
public record StaffMemberRequest(String name, String role, String email, Boolean isActive, Instant hiredAt) {

    public boolean activeOrDefault() {
        return isActive == null || isActive;
    }
}
