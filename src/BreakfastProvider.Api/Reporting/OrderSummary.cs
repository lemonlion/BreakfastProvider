namespace BreakfastProvider.Api.Reporting;

public class OrderSummary
{
    public int Id { get; set; }
    public Guid OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public int? TableNumber { get; set; }
    public string Status { get; set; } = "Created";
    public DateTime CreatedAt { get; set; }
}
