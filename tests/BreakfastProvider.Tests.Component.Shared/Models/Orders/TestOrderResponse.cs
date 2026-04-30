namespace BreakfastProvider.Tests.Component.Shared.Models.Orders;

public class TestOrderResponse
{
    public Guid OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public List<TestOrderItemResponse> Items { get; set; } = [];
    public int? TableNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class TestOrderItemResponse
{
    public string ItemType { get; set; } = string.Empty;
    public Guid BatchId { get; set; }
    public int Quantity { get; set; }
}
