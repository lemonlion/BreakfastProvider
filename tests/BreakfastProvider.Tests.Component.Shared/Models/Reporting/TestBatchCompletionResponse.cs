namespace BreakfastProvider.Tests.Component.Shared.Models.Reporting;

public class TestBatchCompletionResponse
{
    public Guid BatchId { get; set; }
    public string RecipeType { get; set; } = string.Empty;
    public string Ingredients { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }
}
