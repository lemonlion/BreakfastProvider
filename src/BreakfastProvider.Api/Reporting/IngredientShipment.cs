namespace BreakfastProvider.Api.Reporting;

/// <summary>An ingredient delivery, from an IngredientDeliveryEvent that Azure Event Grid delivers.</summary>
public class IngredientShipment
{
    /// <summary>The reporting database's key for the row.</summary>
    public int Id { get; set; }

    /// <summary>The delivery's id, as the event gives it.</summary>
    public Guid DeliveryId { get; set; }

    /// <summary>The ingredient delivered.</summary>
    public string IngredientName { get; set; } = string.Empty;

    /// <summary>How much was delivered, as the event states it.</summary>
    public decimal Quantity { get; set; }

    /// <summary>When the delivery arrived, as the event states it.</summary>
    public DateTime DeliveredAt { get; set; }
}
