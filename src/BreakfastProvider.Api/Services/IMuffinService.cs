using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;

namespace BreakfastProvider.Api.Services;

public interface IMuffinService
{
    Task<MuffinResponse> MakeMuffinsAsync(MuffinRequest request, CancellationToken cancellationToken = default);
}
