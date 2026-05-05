namespace BreakfastProvider.Tests.Component.Shared.Models.Muffins;

/// <summary>
/// Flat test data model for MemberData parameterisation.
/// Baking profile fields are flattened to top-level for visibility in test explorers.
/// </summary>
public record MuffinRecipeTestData
{
    public required IngredientSet Ingredients { get; init; }
    public required int Temperature { get; init; }
    public required int DurationMinutes { get; init; }
    public required string PanType { get; init; }
    public List<ToppingData>? Toppings { get; init; }
}

public record IngredientSet
{
    public required string Flour { get; init; }
    public required string Apples { get; init; }
    public required string Cinnamon { get; init; }
}

public record ToppingData
{
    public required string Name { get; init; }
    public required string Amount { get; init; }
}

public record MuffinRecipeTestDataWithoutToppings
{
    public required IngredientSet Ingredients { get; init; }
    public required int Temperature { get; init; }
    public required int DurationMinutes { get; init; }
    public required string PanType { get; init; }
}

public record MuffinBatchExpectation
{
    public required int ExpectedIngredientCount { get; init; }
    public required int ExpectedToppingCount { get; init; }
    public required bool HasBakingInfo { get; init; }
}
