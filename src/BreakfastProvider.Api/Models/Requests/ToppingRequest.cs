namespace BreakfastProvider.Api.Models.Requests;

public record ToppingRequest
{
    public string? Name { get; init; }

    public string? Category { get; init; }
}
