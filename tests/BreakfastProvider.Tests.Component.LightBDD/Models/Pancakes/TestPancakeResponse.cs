namespace BreakfastProvider.Tests.Component.LightBDD.Models.Pancakes;

public class TestPancakeResponse
{
    public Guid BatchId { get; set; }
    public List<string> Ingredients { get; set; } = [];
    public List<string> Toppings { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}
