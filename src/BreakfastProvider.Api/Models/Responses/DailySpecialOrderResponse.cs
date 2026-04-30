namespace BreakfastProvider.Api.Models.Responses;

public record DailySpecialOrderResponse
{
    public Guid OrderConfirmationId { get; init; }
    public Guid SpecialId { get; init; }
    public int QuantityOrdered { get; init; }
    public int RemainingQuantity { get; init; }
}
