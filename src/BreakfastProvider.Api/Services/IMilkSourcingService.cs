using BreakfastProvider.Api.Models.Responses;

namespace BreakfastProvider.Api.Services;

public interface IMilkSourcingService
{
    Task<MilkResponse> SourceFromCowAsync(CancellationToken cancellationToken = default);
    Task<GoatMilkResponse> SourceFromGoatAsync(CancellationToken cancellationToken = default);
}
