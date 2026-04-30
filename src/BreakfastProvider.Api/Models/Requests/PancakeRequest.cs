namespace BreakfastProvider.Api.Models.Requests;

public record PancakeRequest
{
    public string? Milk { get; init; }

    public string? Flour { get; init; }

    public string? Eggs { get; init; }

    public List<string> Toppings { get; init; } = [];
}
