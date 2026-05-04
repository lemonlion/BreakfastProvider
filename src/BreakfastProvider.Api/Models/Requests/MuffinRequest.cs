namespace BreakfastProvider.Api.Models.Requests;

public record MuffinRequest
{
    public string? Milk { get; init; }

    public string? Flour { get; init; }

    public string? Eggs { get; init; }

    public string? Apples { get; init; }

    public string? Cinnamon { get; init; }

    public BakingProfile? Baking { get; init; }

    public List<MuffinTopping> Toppings { get; init; } = [];
}

public record BakingProfile
{
    public int Temperature { get; init; }

    public int DurationMinutes { get; init; }

    public string? PanType { get; init; }
}

public record MuffinTopping
{
    public string? Name { get; init; }

    public string? Amount { get; init; }
}
