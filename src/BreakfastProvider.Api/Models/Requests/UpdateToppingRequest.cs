namespace BreakfastProvider.Api.Models.Requests;

public record UpdateToppingRequest
{
    public string? Name { get; init; }

    public string? Category { get; init; }
}
