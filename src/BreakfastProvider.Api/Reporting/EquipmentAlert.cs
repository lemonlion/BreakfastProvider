namespace BreakfastProvider.Api.Reporting;

public class EquipmentAlert
{
    public int Id { get; set; }
    public Guid AlertId { get; set; }
    public Guid BatchId { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public DateTime AlertedAt { get; set; }
}
