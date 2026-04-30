namespace BreakfastProvider.Api.Models.Requests;

public record StaffMemberRequest
{
    public string? Name { get; init; }
    public string? Role { get; init; }
    public string? Email { get; init; }
    public bool IsActive { get; init; } = true;
    public DateTime? HiredAt { get; init; }
}
