namespace BreakfastProvider.Tests.Component.LightBDD.Models.AuditLogs;

public class TestAuditLogResponse
{
    public Guid AuditLogId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
