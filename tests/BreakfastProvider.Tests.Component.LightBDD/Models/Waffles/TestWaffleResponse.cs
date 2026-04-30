namespace BreakfastProvider.Tests.Component.LightBDD.Models.Waffles;

public class TestWaffleResponse
{
    public Guid BatchId { get; set; }
    public List<string> Ingredients { get; set; } = [];
    public List<string> Toppings { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}
