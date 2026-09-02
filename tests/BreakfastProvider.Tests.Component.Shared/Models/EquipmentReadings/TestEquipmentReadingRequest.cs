namespace BreakfastProvider.Tests.Component.Shared.Models.EquipmentReadings;

public class TestEquipmentReadingRequest
{
    public string? EquipmentId { get; set; }
    public string? Metric { get; set; }
    public decimal Value { get; set; }
    public string? Unit { get; set; }
}
