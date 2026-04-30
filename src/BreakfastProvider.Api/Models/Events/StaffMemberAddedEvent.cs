using System.ComponentModel;
using BreakfastProvider.Api.Events;

namespace BreakfastProvider.Api.Models.Events;

[Description("Published when a new staff member is registered in the system.")]
public class StaffMemberAddedEvent : IPubSubEvent
{
    [Description("Staff member ID.")]
    public int StaffId { get; set; }

    [Description("Full name of the staff member.")]
    public string Name { get; set; } = string.Empty;

    [Description("Role assigned to the staff member.")]
    public string Role { get; set; } = string.Empty;

    [Description("Timestamp when the staff member was added (ISO 8601 format).")]
    public DateTime AddedAt { get; set; }
}
