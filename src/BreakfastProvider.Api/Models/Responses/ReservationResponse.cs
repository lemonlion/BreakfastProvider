namespace BreakfastProvider.Api.Models.Responses;

public record ReservationResponse
{
    public int Id { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public int TableNumber { get; init; }
    public int PartySize { get; init; }
    public DateTime ReservedAt { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? ContactPhone { get; init; }
    public DateTime CreatedAt { get; init; }
}
