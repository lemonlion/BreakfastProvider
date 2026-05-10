using BreakfastProvider.Tests.Component.Shared.Models.AppleCinnamonMuffins;

namespace BreakfastProvider.Tests.Component.Shared.Constants;

public static class MuffinRecipeVariations
{
    private static readonly MuffinRecipeVariation[] _recipeVariations =
    [
        new MuffinRecipeVariation
        {
            RecipeName = "Classic",
            Recipe = new MuffinRecipeTestData
            {
                Ingredients = new IngredientSet
                {
                    Flour = "Plain Flour",
                    Apples = "Granny Smith",
                    Cinnamon = "Ceylon"
                },
                Toppings =
                [
                    new ToppingData { Name = "Streusel", Amount = "Light" },
                    new ToppingData { Name = "Icing Glaze", Amount = "Drizzle" }
                ]
            },
            Temperature = 180,
            DurationMinutes = 25,
            PanType = "Standard",
            Expected = new MuffinBatchExpectation
            {
                ExpectedIngredientCount = 5,
                ExpectedToppingCount = 2,
                HasBakingInfo = true
            }
        },
        new MuffinRecipeVariation
        {
            RecipeName = "Rustic Wholesome",
            Recipe = new MuffinRecipeTestData
            {
                Ingredients = new IngredientSet
                {
                    Flour = "Whole Wheat",
                    Apples = "Honeycrisp",
                    Cinnamon = "Cassia"
                },
                Toppings =
                [
                    new ToppingData { Name = "Brown Sugar Crumb", Amount = "Heavy" },
                    new ToppingData { Name = "Maple Drizzle", Amount = "Light" }
                ]
            },
            Temperature = 175,
            DurationMinutes = 30,
            PanType = "Cast Iron",
            Expected = new MuffinBatchExpectation
            {
                ExpectedIngredientCount = 5,
                ExpectedToppingCount = 2,
                HasBakingInfo = true
            }
        },
        new MuffinRecipeVariation
        {
            RecipeName = "Spiced Deluxe",
            Recipe = new MuffinRecipeTestData
            {
                Ingredients = new IngredientSet
                {
                    Flour = "Almond Flour",
                    Apples = "Pink Lady",
                    Cinnamon = "Saigon"
                },
                Toppings =
                [
                    new ToppingData { Name = "Cinnamon Sugar", Amount = "Heavy" },
                    new ToppingData { Name = "Cream Cheese Swirl", Amount = "Thick" }
                ]
            },
            Temperature = 190,
            DurationMinutes = 20,
            PanType = "Silicone",
            Expected = new MuffinBatchExpectation
            {
                ExpectedIngredientCount = 5,
                ExpectedToppingCount = 2,
                HasBakingInfo = true
            }
        }
    ];

    public static IEnumerable<object[]> RecipeVariations =>
        _recipeVariations.Select(v => new object[] { v.RecipeName, v.Recipe, v.Temperature, v.DurationMinutes, v.PanType, v.Expected });
}
