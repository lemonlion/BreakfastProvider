namespace BreakfastProvider.Api.Models.Responses;

public record ToppingResponse
{
    public Guid ToppingId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
}
