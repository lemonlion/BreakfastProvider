namespace BreakfastProvider.Api.Models.Requests;

public record ReservationRequest
{
    public string? CustomerName { get; init; }
    public int TableNumber { get; init; }
    public int PartySize { get; init; }
    public DateTime ReservedAt { get; init; }
    public string? ContactPhone { get; init; }
}
