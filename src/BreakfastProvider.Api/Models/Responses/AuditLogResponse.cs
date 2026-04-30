namespace BreakfastProvider.Api.Models.Responses;

public record AuditLogResponse
{
    public Guid AuditLogId { get; init; }
    public string Action { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public Guid EntityId { get; init; }
    public string Details { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}
