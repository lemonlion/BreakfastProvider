using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;

namespace BreakfastProvider.Api.Services;

public interface IInventoryService
{
    Task<InventoryItemResponse> CreateAsync(InventoryItemRequest request, CancellationToken cancellationToken = default);
    Task<InventoryItemResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<InventoryItemResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<InventoryItemResponse?> UpdateAsync(int id, InventoryItemRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
