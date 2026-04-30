namespace BreakfastProvider.Tests.Component.LightBDD.Models.DailySpecials;

public class TestDailySpecialOrderResponse
{
    public Guid OrderConfirmationId { get; set; }
    public Guid SpecialId { get; set; }
    public int QuantityOrdered { get; set; }
    public int RemainingQuantity { get; set; }
}
