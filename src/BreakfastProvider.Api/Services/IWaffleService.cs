using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;

namespace BreakfastProvider.Api.Services;

public interface IWaffleService
{
    Task<WaffleResponse> MakeWafflesAsync(WaffleRequest request, CancellationToken cancellationToken = default);
}
