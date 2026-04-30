namespace BreakfastProvider.Tests.Component.Shared.Models.Reservations;

public class TestReservationResponse
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int TableNumber { get; set; }
    public int PartySize { get; set; }
    public DateTime ReservedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public DateTime CreatedAt { get; set; }
}
