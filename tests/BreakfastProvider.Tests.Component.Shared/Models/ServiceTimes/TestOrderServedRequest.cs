namespace BreakfastProvider.Tests.Component.Shared.Models.ServiceTimes;

public class TestOrderServedRequest
{
    public Guid? OrderId { get; set; }
    public string? ItemType { get; set; }
    public decimal WaitSeconds { get; set; }
}
