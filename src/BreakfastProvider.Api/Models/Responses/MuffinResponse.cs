namespace BreakfastProvider.Api.Models.Responses;

public record MuffinResponse
{
    public Guid BatchId { get; init; }
    public List<string> Ingredients { get; init; } = [];
    public List<string> Toppings { get; init; } = [];
    public int BakingTemperature { get; init; }
    public int BakingDuration { get; init; }
    public DateTime CreatedAt { get; init; }
}
