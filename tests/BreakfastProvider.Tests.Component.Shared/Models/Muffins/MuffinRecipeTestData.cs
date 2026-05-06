namespace BreakfastProvider.Tests.Component.Shared.Models.Muffins;

public record MuffinRecipeTestData
{
    public required IngredientSet Ingredients { get; init; }
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
}

public record MuffinBatchExpectation
{
    public required int ExpectedIngredientCount { get; init; }
    public required int ExpectedToppingCount { get; init; }
    public required bool HasBakingInfo { get; init; }
}

public record MuffinRecipeVariation
{
    public required string RecipeName { get; init; }
    public required MuffinRecipeTestData Recipe { get; init; }
    public required int Temperature { get; init; }
    public required int DurationMinutes { get; init; }
    public required string PanType { get; init; }
    public required MuffinBatchExpectation Expected { get; init; }
}

public record MuffinRecipeVariationWithoutToppings
{
    public required string RecipeName { get; init; }
    public required MuffinRecipeTestDataWithoutToppings Recipe { get; init; }
    public required int Temperature { get; init; }
    public required int DurationMinutes { get; init; }
    public required string PanType { get; init; }
    public required MuffinBatchExpectation Expected { get; init; }
}
