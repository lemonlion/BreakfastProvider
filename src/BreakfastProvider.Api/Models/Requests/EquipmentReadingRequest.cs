namespace BreakfastProvider.Api.Models.Requests;

public record EquipmentReadingRequest
{
    public string? EquipmentId { get; init; }
    public string? Metric { get; init; }
    public decimal Value { get; init; }
    public string? Unit { get; init; }
}
