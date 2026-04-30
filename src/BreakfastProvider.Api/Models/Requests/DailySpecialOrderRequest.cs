namespace BreakfastProvider.Api.Models.Requests;

public record DailySpecialOrderRequest
{
    public Guid? SpecialId { get; init; }
    public int Quantity { get; init; }
}
