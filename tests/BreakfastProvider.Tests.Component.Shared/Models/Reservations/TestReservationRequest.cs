namespace BreakfastProvider.Tests.Component.Shared.Models.Reservations;

public class TestReservationRequest
{
    public string? CustomerName { get; set; }
    public int TableNumber { get; set; }
    public int PartySize { get; set; }
    public DateTime ReservedAt { get; set; }
    public string? ContactPhone { get; set; }
}
