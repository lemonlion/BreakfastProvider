namespace BreakfastProvider.Tests.Component.LightBDD.Models.Reservations;

public class TestReservationRequest
{
    public string? CustomerName { get; set; }
    public int TableNumber { get; set; }
    public int PartySize { get; set; }
    public DateTime ReservedAt { get; set; }
    public string? ContactPhone { get; set; }
}
