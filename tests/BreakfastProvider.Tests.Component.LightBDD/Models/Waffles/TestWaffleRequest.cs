namespace BreakfastProvider.Tests.Component.LightBDD.Models.Waffles;

public class TestWaffleRequest
{
    public string? Milk { get; set; }
    public string? Flour { get; set; }
    public string? Eggs { get; set; }
    public string? Butter { get; set; }
    public List<string> Toppings { get; set; } = [];
}