using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;

namespace BreakfastProvider.Api.Services;

public interface IStaffService
{
    Task<StaffMemberResponse> CreateAsync(StaffMemberRequest request, CancellationToken cancellationToken = default);
    Task<StaffMemberResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<StaffMemberResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<StaffMemberResponse?> UpdateAsync(int id, StaffMemberRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
