using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Storage;
using Microsoft.AspNetCore.Mvc;

namespace BreakfastProvider.Api.Controllers;

[ApiController]
[Route("audit-logs")]
[Produces("application/json")]
public class AuditLogController(ICosmosRepository<AuditLogDocument> auditLogRepository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AuditLogResponse>>> GetAuditLogs([FromQuery] string? entityType = null, [FromQuery] Guid? entityId = null, CancellationToken cancellationToken = default)
    {
        // Pre-extract nullable value type members so the Cosmos SDK LINQ evaluator
        // doesn't access .Value/.HasValue on a null-boxed Guid? (which throws TargetException).
        var hasEntityId = entityId.HasValue;
        var entityIdValue = entityId.GetValueOrDefault();

        var documents = await auditLogRepository.QueryAsync(d =>
            (entityType == null || d.EntityType == entityType) &&
            (!hasEntityId || d.EntityId == entityIdValue),
            cancellationToken);

        var results = documents.Select(d => new AuditLogResponse
        {
            AuditLogId = d.AuditLogId,
            Action = d.Action,
            EntityType = d.EntityType,
            EntityId = d.EntityId,
            Details = d.Details,
            Timestamp = d.Timestamp
        }).OrderByDescending(r => r.Timestamp).ToList();

        return results;
    }
}
