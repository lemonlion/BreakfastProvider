using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Muffins;
using BreakfastProvider.Tests.Component.Shared.Common.Validation;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Muffins;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;
using TestTrackingDiagrams.NUnit4;

namespace BreakfastProvider.Tests.Component.NUnit.Scenarios.Muffins;

public class Apple_Cinnamon_Muffins_Creation_Tests : BaseFixture
{
    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostMuffinsSteps _muffinSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;

    public Apple_Cinnamon_Muffins_Creation_Tests()
    {
        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _muffinSteps = Get<PostMuffinsSteps>();
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

    [Test]
    [HappyPath]
    public async Task Valid_apple_cinnamon_muffin_request_should_return_a_fresh_batch()
    {
        // Given a valid muffin recipe with all ingredients
        await _milkSteps.Retrieve();
        _milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        _muffinSteps.Request.Milk = _milkSteps.MilkResponse.Milk;

        await _eggsSteps.Retrieve();
        _eggsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        _muffinSteps.Request.Eggs = _eggsSteps.EggsResponse.Eggs;

        await _flourSteps.Retrieve();
        _flourSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        _muffinSteps.Request.Flour = _flourSteps.FlourResponse.Flour;

        _muffinSteps.Request.Apples = MuffinDefaults.GrannySmithApples;
        _muffinSteps.Request.Cinnamon = MuffinDefaults.CeylonCinnamon;
        _muffinSteps.Request.Baking = new TestBakingProfile
        {
            Temperature = MuffinDefaults.DefaultTemperature,
            DurationMinutes = MuffinDefaults.DefaultDuration,
            PanType = MuffinDefaults.DefaultPanType
        };
        _muffinSteps.Request.Toppings = [new TestMuffinTopping { Name = "Streusel", Amount = "Light" }];

        // When the muffins are prepared
        await _muffinSteps.Send();

        // Then the response should contain a valid batch with all ingredients
        _muffinSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _muffinSteps.ParseResponse();
        _muffinSteps.Response!.Ingredients.Should().Contain(_milkSteps.MilkResponse.Milk);
        _muffinSteps.Response!.Ingredients.Should().Contain(_eggsSteps.EggsResponse.Eggs);
        _muffinSteps.Response!.Ingredients.Should().Contain(_flourSteps.FlourResponse.Flour);
        _muffinSteps.Response!.Ingredients.Should().Contain(MuffinDefaults.GrannySmithApples);
        _muffinSteps.Response!.Ingredients.Should().Contain(MuffinDefaults.CeylonCinnamon);
        _muffinSteps.Response!.Toppings.Should().HaveCount(1);
        _muffinSteps.Response!.BakingTemperature.Should().Be(MuffinDefaults.DefaultTemperature);
        _muffinSteps.Response!.BakingDuration.Should().Be(MuffinDefaults.DefaultDuration);

        // And the cow service should have received a milk request
        if (!Settings.RunAgainstExternalServiceUnderTest)
            _downstreamSteps.AssertCowServiceReceivedMilkRequest();
    }

    
    [TestCaseSource(typeof(MuffinRecipeVariations), nameof(MuffinRecipeVariations.RecipeVariations))]
    public async Task Different_muffin_recipes_should_produce_the_expected_batch(
        string recipeName, MuffinRecipeTestData recipe, int temperature, int durationMinutes, string panType, MuffinBatchExpectation expected)
    {
        // Given a muffin recipe with specific ingredients and baking profile
        await _milkSteps.Retrieve();
        _milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        _muffinSteps.Request.Milk = _milkSteps.MilkResponse.Milk;

        await _eggsSteps.Retrieve();
        _eggsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        _muffinSteps.Request.Eggs = _eggsSteps.EggsResponse.Eggs;

        _muffinSteps.Request.Flour = recipe.Ingredients.Flour;
        _muffinSteps.Request.Apples = recipe.Ingredients.Apples;
        _muffinSteps.Request.Cinnamon = recipe.Ingredients.Cinnamon;
        _muffinSteps.Request.Baking = new TestBakingProfile
        {
            Temperature = temperature,
            DurationMinutes = durationMinutes,
            PanType = panType
        };
        _muffinSteps.Request.Toppings = recipe.Toppings?
            .Select(t => new TestMuffinTopping { Name = t.Name, Amount = t.Amount })
            .ToList();

        // When the muffins are prepared
        await _muffinSteps.Send();

        // Then the batch should match expectations
        _muffinSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _muffinSteps.ParseResponse();
        _muffinSteps.Response!.Ingredients.Should().HaveCount(expected.ExpectedIngredientCount);
        _muffinSteps.Response!.Toppings.Should().HaveCount(expected.ExpectedToppingCount);
        (expected.HasBakingInfo
            ? _muffinSteps.Response!.BakingTemperature > 0
            : _muffinSteps.Response!.BakingTemperature == 0).Should().BeTrue();
    }

    
    [TestCaseSource(typeof(MuffinRecipeVariations), nameof(MuffinRecipeVariations.RecipeVariationsWithoutToppings))]
    public async Task Different_muffin_recipes_without_toppings_should_produce_the_expected_batch(
        string recipeName, MuffinRecipeTestDataWithoutToppings recipe, int temperature, int durationMinutes, string panType, MuffinBatchExpectation expected)
    {
        // Given a muffin recipe with specific ingredients and baking profile but no toppings
        await _milkSteps.Retrieve();
        _milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        _muffinSteps.Request.Milk = _milkSteps.MilkResponse.Milk;

        await _eggsSteps.Retrieve();
        _eggsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        _muffinSteps.Request.Eggs = _eggsSteps.EggsResponse.Eggs;

        _muffinSteps.Request.Flour = recipe.Ingredients.Flour;
        _muffinSteps.Request.Apples = recipe.Ingredients.Apples;
        _muffinSteps.Request.Cinnamon = recipe.Ingredients.Cinnamon;
        _muffinSteps.Request.Baking = new TestBakingProfile
        {
            Temperature = temperature,
            DurationMinutes = durationMinutes,
            PanType = panType
        };

        // When the muffins are prepared
        await _muffinSteps.Send();

        // Then the batch should match expectations
        _muffinSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _muffinSteps.ParseResponse();
        _muffinSteps.Response!.Ingredients.Should().HaveCount(expected.ExpectedIngredientCount);
        _muffinSteps.Response!.Toppings.Should().HaveCount(expected.ExpectedToppingCount);
        (expected.HasBakingInfo
            ? _muffinSteps.Response!.BakingTemperature > 0
            : _muffinSteps.Response!.BakingTemperature == 0).Should().BeTrue();
    }
    [TestCase("Flour", "", "Flour is required", "'Flour' is required.", "Bad Request")]
    [TestCase("Apples", "", "Apples is required", "'Apples' is required.", "Bad Request")]
    [TestCase("Cinnamon", "", "Cinnamon is required", "'Cinnamon' is required.", "Bad Request")]
    [TestCase("Milk", "", "Milk is required", "'Milk' is required.", "Bad Request")]
    [TestCase("Eggs", "", "Eggs is required", "'Eggs' is required.", "Bad Request")]
    [TestCase("Cinnamon", "<script>alert('xss')</script>", "XSS in cinnamon", "Cinnamon contains potentially dangerous content.", "Bad Request")]
    public async Task Muffin_request_with_invalid_field_should_return_bad_request(
        string field, string value, string reason, string expectedError, string expectedStatus)
    {
        // Given a valid muffin request with an invalid field
        var validBase = new TestMuffinRequest
        {
            Milk = CowServiceDefaults.FreshMilk,
            Flour = IngredientDefaults.PlainFlour,
            Eggs = IngredientDefaults.FreeRangeEggs,
            Apples = MuffinDefaults.GrannySmithApples,
            Cinnamon = MuffinDefaults.CeylonCinnamon,
            Baking = new TestBakingProfile
            {
                Temperature = MuffinDefaults.DefaultTemperature,
                DurationMinutes = MuffinDefaults.DefaultDuration,
                PanType = MuffinDefaults.DefaultPanType
            },
            Toppings = [new TestMuffinTopping { Name = "Streusel", Amount = "Light" }]
        };

        var input = new InvalidFieldFromRequest(field, value, reason);
        var requests = ValidationHelper.CreateValidationRequests(validBase, [input]);

        // When the invalid muffin request is submitted
        var responses = await ValidationHelper.SendValidationRequests(
            Client, RequestId, Endpoints.Muffins, requests, [input]);

        // Then the response should contain the validation error
        var actualResults = await ValidationHelper.ParseValidationResponses(responses);
        var actual = actualResults.Single();
        actual.ErrorMessage.Should().Contain(expectedError);
        actual.ResponseStatus.Should().Be(expectedStatus);
    }
}
