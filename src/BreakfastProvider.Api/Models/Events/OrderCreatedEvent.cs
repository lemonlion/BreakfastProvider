namespace BreakfastProvider.Api.Models.Events;

public class OrderCreatedEvent
{
    public Guid OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public int? TableNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}
