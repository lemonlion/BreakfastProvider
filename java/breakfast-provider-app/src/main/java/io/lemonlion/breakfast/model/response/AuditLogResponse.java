package io.lemonlion.breakfast.model.response;

import java.time.Instant;
import java.util.UUID;

/** Twin of C# {@code AuditLogResponse}. */
public record AuditLogResponse(
        UUID auditLogId, String action, String entityType, UUID entityId, String details, Instant timestamp) {
}
