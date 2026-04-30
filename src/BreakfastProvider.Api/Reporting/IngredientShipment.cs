namespace BreakfastProvider.Api.Reporting;

public class IngredientShipment
{
    public int Id { get; set; }
    public Guid DeliveryId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public DateTime DeliveredAt { get; set; }
}
