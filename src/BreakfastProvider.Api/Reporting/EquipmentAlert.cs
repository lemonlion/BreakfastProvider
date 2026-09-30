namespace BreakfastProvider.Api.Reporting;

/// <summary>
/// An equipment alert, from Azure Event Hub. Each pancake batch raises one: equipment Griddle, alert type
/// UsageCycleCompleted.
/// </summary>
public class EquipmentAlert
{
    /// <summary>The reporting database's key for the row.</summary>
    public int Id { get; set; }

    /// <summary>The alert's id.</summary>
    public Guid AlertId { get; set; }

    /// <summary>The id of the batch that raised the alert.</summary>
    public Guid BatchId { get; set; }

    /// <summary>The equipment the alert is about, for example Griddle.</summary>
    public string EquipmentName { get; set; } = string.Empty;

    /// <summary>What happened, for example UsageCycleCompleted.</summary>
    public string AlertType { get; set; } = string.Empty;

    /// <summary>When the alert was raised, in UTC.</summary>
    public DateTime AlertedAt { get; set; }
}
