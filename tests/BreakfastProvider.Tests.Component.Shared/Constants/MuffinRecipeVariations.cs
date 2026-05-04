using BreakfastProvider.Tests.Component.Shared.Models.Muffins;

namespace BreakfastProvider.Tests.Component.Shared.Constants;

public static class MuffinRecipeVariations
{
    public static IEnumerable<object[]> RecipeVariations =>
    [
        [
            "Classic",
            new MuffinRecipeTestData
            {
                Ingredients = new IngredientSet
                {
                    Flour = "Plain Flour",
                    Apples = "Granny Smith",
                    Cinnamon = "Ceylon"
                },
                Baking = new BakingProfileData
                {
                    Temperature = 180,
                    DurationMinutes = 25,
                    PanType = "Standard"
                },
                Toppings =
                [
                    new ToppingData { Name = "Streusel", Amount = "Light" },
                    new ToppingData { Name = "Icing Glaze", Amount = "Drizzle" }
                ]
            },
            new MuffinBatchExpectation
            {
                ExpectedIngredientCount = 5,
                ExpectedToppingCount = 2,
                HasBakingInfo = true
            }
        ],
        [
            "Rustic Wholesome",
            new MuffinRecipeTestData
            {
                Ingredients = new IngredientSet
                {
                    Flour = "Whole Wheat",
                    Apples = "Honeycrisp",
                    Cinnamon = "Cassia"
                },
                Baking = new BakingProfileData
                {
                    Temperature = 175,
                    DurationMinutes = 30,
                    PanType = "Cast Iron"
                },
                Toppings =
                [
                    new ToppingData { Name = "Brown Sugar Crumb", Amount = "Heavy" },
                    new ToppingData { Name = "Maple Drizzle", Amount = "Light" }
                ]
            },
            new MuffinBatchExpectation
            {
                ExpectedIngredientCount = 5,
                ExpectedToppingCount = 2,
                HasBakingInfo = true
            }
        ],
        [
            "Spiced Deluxe",
            new MuffinRecipeTestData
            {
                Ingredients = new IngredientSet
                {
                    Flour = "Almond Flour",
                    Apples = "Pink Lady",
                    Cinnamon = "Saigon"
                },
                Baking = new BakingProfileData
                {
                    Temperature = 190,
                    DurationMinutes = 20,
                    PanType = "Silicone"
                },
                Toppings =
                [
                    new ToppingData { Name = "Cinnamon Sugar", Amount = "Heavy" },
                    new ToppingData { Name = "Cream Cheese Swirl", Amount = "Thick" }
                ]
            },
            new MuffinBatchExpectation
            {
                ExpectedIngredientCount = 5,
                ExpectedToppingCount = 2,
                HasBakingInfo = true
            }
        ]
    ];
}
