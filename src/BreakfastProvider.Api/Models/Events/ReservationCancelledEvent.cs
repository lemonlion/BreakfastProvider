using System.ComponentModel;
using BreakfastProvider.Api.Events;

namespace BreakfastProvider.Api.Models.Events;

[Description("Published when a reservation is cancelled.")]
public class ReservationCancelledEvent : IPubSubEvent
{
    [Description("Reservation ID.")]
    public int ReservationId { get; set; }

    [Description("Customer name for the reservation.")]
    public string CustomerName { get; set; } = string.Empty;

    [Description("Reason for cancellation.")]
    public string Reason { get; set; } = string.Empty;

    [Description("Timestamp when the reservation was cancelled (ISO 8601 format).")]
    public DateTime CancelledAt { get; set; }
}
