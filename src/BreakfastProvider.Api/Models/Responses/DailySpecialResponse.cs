namespace BreakfastProvider.Api.Models.Responses;

public record DailySpecialResponse
{
    public Guid SpecialId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int RemainingQuantity { get; init; }
}
