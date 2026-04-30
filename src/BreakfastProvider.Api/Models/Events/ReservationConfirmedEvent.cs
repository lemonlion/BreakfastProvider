using System.ComponentModel;
using BreakfastProvider.Api.Events;

namespace BreakfastProvider.Api.Models.Events;

[Description("Published when a reservation is confirmed.")]
public class ReservationConfirmedEvent : IPubSubEvent
{
    [Description("Reservation ID.")]
    public int ReservationId { get; set; }

    [Description("Customer name for the reservation.")]
    public string CustomerName { get; set; } = string.Empty;

    [Description("Number of guests.")]
    public int PartySize { get; set; }

    [Description("Reserved date and time (ISO 8601 format).")]
    public DateTime ReservedAt { get; set; }

    [Description("Timestamp when the reservation was confirmed (ISO 8601 format).")]
    public DateTime ConfirmedAt { get; set; }
}
