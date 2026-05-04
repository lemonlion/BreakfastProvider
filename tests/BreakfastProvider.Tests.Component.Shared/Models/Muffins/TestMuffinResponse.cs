namespace BreakfastProvider.Tests.Component.Shared.Models.Muffins;

public class TestMuffinResponse
{
    public Guid BatchId { get; set; }
    public List<string> Ingredients { get; set; } = [];
    public List<string> Toppings { get; set; } = [];
    public int BakingTemperature { get; set; }
    public int BakingDuration { get; set; }
    public DateTime CreatedAt { get; set; }
}
