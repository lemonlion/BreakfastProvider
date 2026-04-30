namespace BreakfastProvider.Tests.Component.LightBDD.Models.Reporting;

public class TestOrderSummaryResponse
{
    public int Id { get; set; }
    public Guid OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public int? TableNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
