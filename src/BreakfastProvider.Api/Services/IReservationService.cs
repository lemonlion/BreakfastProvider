using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;

namespace BreakfastProvider.Api.Services;

public interface IReservationService
{
    Task<ReservationResponse> CreateAsync(ReservationRequest request, CancellationToken cancellationToken = default);
    Task<ReservationResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<ReservationResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<ReservationResponse?> UpdateAsync(int id, ReservationRequest request, CancellationToken cancellationToken = default);
    Task<(ReservationResponse? Reservation, string? Error)> CancelAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
