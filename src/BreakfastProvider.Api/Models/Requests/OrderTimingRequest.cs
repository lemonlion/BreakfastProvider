namespace BreakfastProvider.Api.Models.Requests;

public record OrderTimingRequest
{
    public string? OrderId { get; init; }
    public string? Station { get; init; }
    public string? ItemType { get; init; }
    public decimal PrepSeconds { get; init; }
}
