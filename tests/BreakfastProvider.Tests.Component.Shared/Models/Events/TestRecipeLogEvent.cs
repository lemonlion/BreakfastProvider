namespace BreakfastProvider.Tests.Component.Shared.Models.Events;

public class TestRecipeLogEvent
{
    public Guid OrderId { get; set; }
    public string RecipeType { get; set; } = string.Empty;
    public List<string> Ingredients { get; set; } = [];
    public List<string> Toppings { get; set; } = [];
    public DateTime LoggedAt { get; set; }
}
