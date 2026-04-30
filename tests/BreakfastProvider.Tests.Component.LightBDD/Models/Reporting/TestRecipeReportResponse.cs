namespace BreakfastProvider.Tests.Component.LightBDD.Models.Reporting;

public class TestRecipeReportResponse
{
    public int Id { get; set; }
    public Guid OrderId { get; set; }
    public string RecipeType { get; set; } = string.Empty;
    public string Ingredients { get; set; } = string.Empty;
    public string Toppings { get; set; } = string.Empty;
    public DateTime LoggedAt { get; set; }
}
