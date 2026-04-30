namespace BreakfastProvider.Api.Models.Requests;

public record UpdateOrderStatusRequest
{
    public string? Status { get; init; }
}
