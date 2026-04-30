namespace BreakfastProvider.Api.Models.Responses;

public record MenuItemResponse
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsAvailable { get; init; }
    public List<string> RequiredIngredients { get; init; } = [];
}
