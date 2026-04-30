namespace BreakfastProvider.Api.Models.Requests;

public record OrderRequest
{
    public string? CustomerName { get; init; }

    public List<OrderItemRequest> Items { get; init; } = [];

    public int? TableNumber { get; init; }
}

public record OrderItemRequest
{
    public string? ItemType { get; init; }

    public Guid? BatchId { get; init; }

    public int Quantity { get; init; } = 1;
}
