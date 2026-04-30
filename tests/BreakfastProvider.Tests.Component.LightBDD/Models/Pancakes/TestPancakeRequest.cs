namespace BreakfastProvider.Tests.Component.LightBDD.Models.Pancakes;

public class TestPancakeRequest
{
    public string? Milk { get; set; }
    public string? Flour { get; set; }
    public string? Eggs { get; set; }
    public List<string> Toppings { get; set; } = [];
}
