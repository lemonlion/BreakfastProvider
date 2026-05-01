using BreakfastProvider.Api.Models.Responses;

namespace BreakfastProvider.Api.Services;

public interface IMenuService
{
    Task<(List<MenuItemResponse> Items, bool FromCache)> GetMenuAsync(CancellationToken cancellationToken = default);
    void ClearCache();
}
