namespace BreakfastProvider.Tests.Component.Shared.Models.OrderTimings;

public class TestOrderTimingRequest
{
    public string? OrderId { get; set; }
    public string? Station { get; set; }
    public string? ItemType { get; set; }
    public decimal PrepSeconds { get; set; }
}
