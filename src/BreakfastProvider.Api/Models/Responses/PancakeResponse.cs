namespace BreakfastProvider.Api.Models.Responses;

public record PancakeResponse
{
    public Guid BatchId { get; init; }
    public List<string> Ingredients { get; init; } = [];
    public List<string> Toppings { get; init; } = [];
    public DateTime CreatedAt { get; init; }
}
