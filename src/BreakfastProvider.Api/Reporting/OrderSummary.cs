namespace BreakfastProvider.Api.Reporting;

/// <summary>An order as the reporting database records it when the order is created.</summary>
public class OrderSummary
{
    /// <summary>The reporting database's key for the row.</summary>
    public int Id { get; set; }

    /// <summary>The order's id, as returned by POST /orders.</summary>
    public Guid OrderId { get; set; }

    /// <summary>The customer the order is for.</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>How many items the order has.</summary>
    public int ItemCount { get; set; }

    /// <summary>The table the order is for, when it has one.</summary>
    public int? TableNumber { get; set; }

    /// <summary>The order's status when it was recorded, Created. Later changes of status are not recorded here.</summary>
    public string Status { get; set; } = "Created";

    /// <summary>When the order was created, in UTC.</summary>
    public DateTime CreatedAt { get; set; }
}
