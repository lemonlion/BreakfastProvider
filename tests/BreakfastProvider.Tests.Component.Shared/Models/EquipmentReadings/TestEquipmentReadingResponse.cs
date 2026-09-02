namespace BreakfastProvider.Tests.Component.Shared.Models.EquipmentReadings;

public class TestEquipmentReadingResponse
{
    public string ReadingId { get; set; } = string.Empty;
    public string EquipmentId { get; set; } = string.Empty;
    public string Metric { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
}
