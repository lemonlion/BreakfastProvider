using System.ComponentModel;
using BreakfastProvider.Api.Events;

namespace BreakfastProvider.Api.Models.Events;

[Description("Published when a menu item becomes available or unavailable.")]
public class MenuAvailabilityChangedEvent : IPubSubEvent
{
    [Description("Name of the menu item.")]
    public string ItemName { get; set; } = string.Empty;

    [Description("Whether the item is now available.")]
    public bool IsAvailable { get; set; }

    [Description("Reason for the availability change.")]
    public string Reason { get; set; } = string.Empty;

    [Description("Timestamp of the change (ISO 8601 format).")]
    public DateTime ChangedAt { get; set; }
}
