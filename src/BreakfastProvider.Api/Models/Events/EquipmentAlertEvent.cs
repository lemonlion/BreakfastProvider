using System.ComponentModel;
using BreakfastProvider.Api.Events;

namespace BreakfastProvider.Api.Models.Events;

[Description("Published when kitchen equipment is used during batch preparation.")]
public class EquipmentAlertEvent : IEventHubEvent
{
    [Description("Unique alert identifier.")]
    public Guid AlertId { get; set; }

    [Description("Batch ID that triggered the alert.")]
    public Guid BatchId { get; set; }

    [Description("Name of the equipment used.")]
    public string EquipmentName { get; set; } = string.Empty;

    [Description("Type of alert (e.g. UsageCycleCompleted, MaintenanceDue).")]
    public string AlertType { get; set; } = string.Empty;

    [Description("Timestamp when the alert was raised (ISO 8601 format).")]
    public DateTime AlertedAt { get; set; }
}
