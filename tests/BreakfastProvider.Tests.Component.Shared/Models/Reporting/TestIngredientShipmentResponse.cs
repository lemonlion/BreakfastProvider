namespace BreakfastProvider.Tests.Component.Shared.Models.Reporting;

public class TestIngredientShipmentResponse
{
    public Guid DeliveryId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public DateTime DeliveredAt { get; set; }
}
