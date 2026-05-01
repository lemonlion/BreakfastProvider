using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;

namespace BreakfastProvider.Api.Services;

public interface IPancakeService
{
    Task<PancakeResponse> MakePancakesAsync(PancakeRequest request, CancellationToken cancellationToken = default);
}
