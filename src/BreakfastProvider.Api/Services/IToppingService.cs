using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;

namespace BreakfastProvider.Api.Services;

public interface IToppingService
{
    List<ToppingResponse> GetAvailableToppings();
    Task<ToppingResponse> CreateToppingAsync(ToppingRequest request, CancellationToken cancellationToken = default);
    ToppingResponse? UpdateTopping(Guid toppingId, UpdateToppingRequest request);
    bool DeleteTopping(Guid toppingId);
}
