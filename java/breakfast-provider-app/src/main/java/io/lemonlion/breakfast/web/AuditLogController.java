package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.model.response.AuditLogResponse;
import io.lemonlion.breakfast.persistence.CosmosRepository;
import io.lemonlion.breakfast.storage.AuditLogDocument;
import java.util.Comparator;
import java.util.List;
import java.util.UUID;
import org.springframework.http.MediaType;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

/** Twin of C# {@code AuditLogController} ({@code /audit-logs}): filtered audit-log query, newest first. */
@RestController
@RequestMapping(path = "/audit-logs", produces = MediaType.APPLICATION_JSON_VALUE)
public class AuditLogController {

    private final CosmosRepository<AuditLogDocument> auditLogRepository;

    public AuditLogController(CosmosRepository<AuditLogDocument> auditLogRepository) {
        this.auditLogRepository = auditLogRepository;
    }

    @GetMapping
    public List<AuditLogResponse> getAuditLogs(
            @RequestParam(required = false) String entityType,
            @RequestParam(required = false) UUID entityId) {
        return auditLogRepository.findAll().stream()
                .filter(d -> entityType == null || entityType.equals(d.getEntityType()))
                .filter(d -> entityId == null || entityId.equals(d.getEntityId()))
                .map(d -> new AuditLogResponse(d.getAuditLogId(), d.getAction(), d.getEntityType(),
                        d.getEntityId(), d.getDetails(), d.getTimestamp()))
                .sorted(Comparator.comparing(AuditLogResponse::timestamp).reversed())
                .toList();
    }
}
