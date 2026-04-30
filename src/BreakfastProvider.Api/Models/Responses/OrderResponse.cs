namespace BreakfastProvider.Api.Models.Responses;

public record OrderResponse
{
    public Guid OrderId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public List<OrderItemResponse> Items { get; init; } = [];
    public int? TableNumber { get; init; }
    public string Status { get; init; } = "Created";
    public DateTime CreatedAt { get; init; }
}

public record OrderItemResponse
{
    public string ItemType { get; init; } = string.Empty;
    public Guid BatchId { get; init; }
    public int Quantity { get; init; }
}
