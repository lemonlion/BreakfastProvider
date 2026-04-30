namespace BreakfastProvider.Tests.Component.Shared.Models.Menu;

public class TestMenuItemResponse
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public List<string> RequiredIngredients { get; set; } = [];
}