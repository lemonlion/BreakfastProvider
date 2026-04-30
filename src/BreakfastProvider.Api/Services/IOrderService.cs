using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;

namespace BreakfastProvider.Api.Services;

public interface IOrderService
{
    Task<OrderResponse> CreateOrderAsync(OrderRequest request, CancellationToken cancellationToken = default);
    Task<OrderResponse?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<OrderResponse>> ListOrdersAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<(OrderResponse? Order, string? Error)> UpdateOrderStatusAsync(Guid orderId, string newStatus, CancellationToken cancellationToken = default);
}
