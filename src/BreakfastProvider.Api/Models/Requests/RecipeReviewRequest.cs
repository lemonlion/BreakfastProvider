namespace BreakfastProvider.Api.Models.Requests;

public record RecipeReviewRequest
{
    public string? RecipeName { get; init; }
    public string? ReviewerName { get; init; }
    public int Rating { get; init; }
    public string? Comments { get; init; }
    public List<string>? Tags { get; init; }
}
