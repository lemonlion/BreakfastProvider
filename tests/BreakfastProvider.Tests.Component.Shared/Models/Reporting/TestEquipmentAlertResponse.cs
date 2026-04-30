namespace BreakfastProvider.Tests.Component.Shared.Models.Reporting;

public class TestEquipmentAlertResponse
{
    public int Id { get; set; }
    public Guid AlertId { get; set; }
    public Guid BatchId { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public DateTime AlertedAt { get; set; }
}
