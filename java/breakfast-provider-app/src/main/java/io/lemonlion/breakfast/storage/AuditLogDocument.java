package io.lemonlion.breakfast.storage;

import java.time.Instant;
import java.util.UUID;

/** Twin of C# {@code AuditLogDocument} — partitioned by entity type (e.g. {@code "Order"}). */
public class AuditLogDocument {

    private String id = UUID.randomUUID().toString();
    private String partitionKey = "";
    private UUID auditLogId;
    private String action = "";
    private String entityType = "";
    private UUID entityId;
    private String details = "";
    private Instant timestamp = Instant.now();

    public String getId() {
        return id;
    }

    public void setId(String id) {
        this.id = id;
    }

    public String getPartitionKey() {
        return partitionKey;
    }

    public void setPartitionKey(String partitionKey) {
        this.partitionKey = partitionKey;
    }

    public UUID getAuditLogId() {
        return auditLogId;
    }

    public void setAuditLogId(UUID auditLogId) {
        this.auditLogId = auditLogId;
    }

    public String getAction() {
        return action;
    }

    public void setAction(String action) {
        this.action = action;
    }

    public String getEntityType() {
        return entityType;
    }

    public void setEntityType(String entityType) {
        this.entityType = entityType;
    }

    public UUID getEntityId() {
        return entityId;
    }

    public void setEntityId(UUID entityId) {
        this.entityId = entityId;
    }

    public String getDetails() {
        return details;
    }

    public void setDetails(String details) {
        this.details = details;
    }

    public Instant getTimestamp() {
        return timestamp;
    }

    public void setTimestamp(Instant timestamp) {
        this.timestamp = timestamp;
    }
}
