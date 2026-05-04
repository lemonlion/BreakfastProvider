using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Muffins;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Muffins;

[FeatureDescription($"/{Endpoints.Muffins} - Creating apple cinnamon muffins with baking profiles and toppings")]
public partial class Muffins__Creation_Feature
{
    [HappyPath]
    [Scenario]
    public async Task A_Valid_Apple_Cinnamon_Muffin_Request_Should_Return_A_Fresh_Batch()
    {
        await Runner.RunScenarioAsync(
            given => A_valid_muffin_recipe_with_all_ingredients(),
            when => The_muffins_are_prepared(),
            then => The_muffin_response_should_contain_a_valid_batch_with_all_ingredients(),
            and => The_cow_service_should_have_received_a_milk_request());
    }

    [Scenario]
    [MemberData(nameof(MuffinRecipeVariations.RecipeVariations), MemberType = typeof(MuffinRecipeVariations))]
    public async Task Different_Muffin_Recipes_Should_Produce_The_Expected_Batch(
        string recipeName, MuffinRecipeTestData recipe, MuffinBatchExpectation expected)
    {
        await Runner.RunScenarioAsync(
            given => A_muffin_recipe_with_ingredients_and_baking_profile(recipeName, recipe),
            when => The_muffins_are_prepared(),
            then => The_muffin_batch_should_match_the_expected_outcome(expected));
    }

    [Scenario]
    [InlineData("Flour", "", "Flour is required", "'Flour' is required.", "Bad Request")]
    [InlineData("Apples", "", "Apples is required", "'Apples' is required.", "Bad Request")]
    [InlineData("Cinnamon", "", "Cinnamon is required", "'Cinnamon' is required.", "Bad Request")]
    [InlineData("Milk", "", "Milk is required", "'Milk' is required.", "Bad Request")]
    [InlineData("Eggs", "", "Eggs is required", "'Eggs' is required.", "Bad Request")]
    [InlineData("Cinnamon", "<script>alert('xss')</script>", "XSS in cinnamon", "Cinnamon contains potentially dangerous content.", "Bad Request")]
    public async Task Muffins_Endpoint_Is_Called_With_An_Invalid_Field_Should_Return_A_Bad_Request_Response(
        string field, string value, string reason, string expectedError, string expectedStatus)
    {
        await Runner.RunScenarioAsync(
            given => A_valid_muffin_request_with_an_invalid_field(field, value),
            when => The_invalid_muffin_request_is_submitted(),
            then => The_muffin_response_should_contain_the_validation_error(expectedError, expectedStatus));
    }
}
