namespace BreakfastProvider.Api.Models.Requests;

public record WaffleRequest
{
    public string? Milk { get; init; }

    public string? Flour { get; init; }

    public string? Eggs { get; init; }

    public string? Butter { get; init; }

    public List<string> Toppings { get; init; } = [];
}
