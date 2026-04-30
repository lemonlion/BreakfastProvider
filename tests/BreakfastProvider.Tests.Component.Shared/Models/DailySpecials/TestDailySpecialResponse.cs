namespace BreakfastProvider.Tests.Component.Shared.Models.DailySpecials;

public class TestDailySpecialResponse
{
    public Guid SpecialId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int RemainingQuantity { get; set; }
}
