namespace BreakfastProvider.Api.Data.Entities;

public class Reservation
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int TableNumber { get; set; }
    public int PartySize { get; set; }
    public DateTime ReservedAt { get; set; }
    public string Status { get; set; } = "Confirmed";
    public string? ContactPhone { get; set; }
    public DateTime CreatedAt { get; set; }
}
