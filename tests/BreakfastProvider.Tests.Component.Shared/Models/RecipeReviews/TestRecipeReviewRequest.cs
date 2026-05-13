namespace BreakfastProvider.Tests.Component.Shared.Models.RecipeReviews;

public class TestRecipeReviewRequest
{
    public string? RecipeName { get; set; }
    public string? ReviewerName { get; set; }
    public int Rating { get; set; }
    public string? Comments { get; set; }
    public List<string>? Tags { get; set; }
}
