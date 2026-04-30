namespace BreakfastProvider.Tests.Component.Shared.Models.Staff;

public class TestStaffMemberRequest
{
    public string? Name { get; set; }
    public string? Role { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? HiredAt { get; set; }
}
