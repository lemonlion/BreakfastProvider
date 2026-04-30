namespace BreakfastProvider.Tests.Component.LightBDD.Models.Orders;

public class TestOrderRequest
{
    public string? CustomerName { get; set; }
    public List<TestOrderItemRequest> Items { get; set; } = [];
    public int? TableNumber { get; set; }
}

public class TestOrderItemRequest
{
    public string? ItemType { get; set; }
    public Guid? BatchId { get; set; }
    public int Quantity { get; set; } = 1;
}
