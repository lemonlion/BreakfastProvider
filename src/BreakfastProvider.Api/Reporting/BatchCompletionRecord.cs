namespace BreakfastProvider.Api.Reporting;

public class BatchCompletionRecord
{
    public int Id { get; set; }
    public Guid BatchId { get; set; }
    public string RecipeType { get; set; } = string.Empty;
    public string Ingredients { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }
}
