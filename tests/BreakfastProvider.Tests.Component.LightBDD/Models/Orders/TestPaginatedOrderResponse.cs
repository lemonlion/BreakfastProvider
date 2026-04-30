namespace BreakfastProvider.Tests.Component.LightBDD.Models.Orders;

public class TestPaginatedOrderResponse
{
    public List<TestOrderResponse> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
