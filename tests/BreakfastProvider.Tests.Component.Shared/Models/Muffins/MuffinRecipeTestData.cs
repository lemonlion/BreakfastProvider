namespace BreakfastProvider.Tests.Component.Shared.Models.Muffins;

/// <summary>
/// Multi-level nested test data model for MemberData parameterisation.
/// Level 0: MuffinRecipeTestData (top-level)
/// Level 1: IngredientSet, BakingProfileData, List&lt;ToppingData&gt;
/// Level 2: Properties within nested objects
/// Level 3: ToppingData items within the list
/// </summary>
public record MuffinRecipeTestData
{
    public required IngredientSet Ingredients { get; init; }
    public required BakingProfileData Baking { get; init; }
    public required List<ToppingData> Toppings { get; init; }
}

public record IngredientSet
{
    public required string Flour { get; init; }
    public required string Apples { get; init; }
    public required string Cinnamon { get; init; }
}

public record BakingProfileData
{
    public required int Temperature { get; init; }
    public required int DurationMinutes { get; init; }
    public required string PanType { get; init; }
}

public record ToppingData
{
    public required string Name { get; init; }
    public required string Amount { get; init; }
}

public record MuffinBatchExpectation
{
    public required int ExpectedIngredientCount { get; init; }
    public required int ExpectedToppingCount { get; init; }
    public required bool HasBakingInfo { get; init; }
}
