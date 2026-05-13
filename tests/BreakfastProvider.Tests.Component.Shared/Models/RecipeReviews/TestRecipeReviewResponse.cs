namespace BreakfastProvider.Tests.Component.Shared.Models.RecipeReviews;

public class TestRecipeReviewResponse
{
    public string ReviewId { get; set; } = string.Empty;
    public string RecipeName { get; set; } = string.Empty;
    public string ReviewerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comments { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}
