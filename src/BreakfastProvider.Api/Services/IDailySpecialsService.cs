using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;

namespace BreakfastProvider.Api.Services;

public interface IDailySpecialsService
{
    List<DailySpecialResponse> GetAvailableSpecials();
    Task<DailySpecialOrderResponse?> CheckIdempotencyAsync(string? idempotencyKey, CancellationToken cancellationToken);
    (Guid Id, string Name, string Description)? ValidateSpecialExists(Guid? specialId);
    DailySpecialOrderResponse? ReserveQuantity(Guid specialId, int quantity, string specialName);
    Task StoreIdempotencyResultAsync(string? idempotencyKey, DailySpecialOrderResponse response, CancellationToken cancellationToken);
    Task PublishOrderEventAsync(DailySpecialOrderResponse response, string specialName, CancellationToken cancellationToken);
    void ResetOrderCounts(Guid? specialId);
}
