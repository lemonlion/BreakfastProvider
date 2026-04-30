namespace BreakfastProvider.Api.Data.Entities;

public class StaffMember
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime HiredAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
