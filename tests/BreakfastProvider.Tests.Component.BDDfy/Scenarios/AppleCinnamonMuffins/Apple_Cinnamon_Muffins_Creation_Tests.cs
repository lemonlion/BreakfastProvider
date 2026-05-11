using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.AppleCinnamonMuffins;
using BreakfastProvider.Tests.Component.Shared.Common.Validation;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.AppleCinnamonMuffins;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.AppleCinnamonMuffins;

public class Apple_Cinnamon_Muffins_Creation_Tests : BaseFixture
{
    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostMuffinsSteps _muffinSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;
    private MuffinRecipeTestData _recipe = null!;
    private int _temperature;
    private int _durationMinutes;
    private string _panType = null!;
    private MuffinBatchExpectation _expected = null!;
    private InvalidFieldFromRequest _input = null!;
    private string _expectedError = null!;
    private string _expectedStatus = null!;
    private VerifiableErrorResult? _actual;

    public Apple_Cinnamon_Muffins_Creation_Tests()
    {
        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _muffinSteps = Get<PostMuffinsSteps>();
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

    [Fact]
    [HappyPath]
    public void Valid_apple_cinnamon_muffin_request_should_return_a_fresh_batch()
    {
        this.Given(x => x.All_ingredients_are_retrieved_for_apple_cinnamon_muffins())
            .When(x => x.The_muffins_are_prepared())
            .Then(x => x.The_response_should_contain_a_valid_batch_with_all_ingredients())
            .And(x => x.The_cow_service_should_have_received_a_milk_request())
            .BDDfy();
    }

    [Theory]
    [MemberData(nameof(MuffinRecipeVariations.RecipeVariations), MemberType = typeof(MuffinRecipeVariations))]
    public void Different_muffin_recipes_should_produce_the_expected_batch(
        string recipeName, MuffinRecipeTestData recipe, int temperature, int durationMinutes, string panType, MuffinBatchExpectation expected)
    {
        _recipe = recipe;
        _temperature = temperature;
        _durationMinutes = durationMinutes;
        _panType = panType;
        _expected = expected;

        this.Given(x => x.All_ingredients_are_retrieved_for_the_muffin_recipe())
            .When(x => x.The_muffins_are_prepared())
            .Then(x => x.The_response_should_match_the_expected_batch())
            .BDDfy();
    }

    [Theory]
    [InlineData("Flour", "", "Flour is required", "'Flour' is required.", "Bad Request")]
    [InlineData("Apples", "", "Apples is required", "'Apples' is required.", "Bad Request")]
    [InlineData("Cinnamon", "", "Cinnamon is required", "'Cinnamon' is required.", "Bad Request")]
    [InlineData("Milk", "", "Milk is required", "'Milk' is required.", "Bad Request")]
    [InlineData("Eggs", "", "Eggs is required", "'Eggs' is required.", "Bad Request")]
    [InlineData("Cinnamon", "<script>alert('xss')</script>", "XSS in cinnamon", "Cinnamon contains potentially dangerous content.", "Bad Request")]
    public void Muffin_request_with_invalid_field_should_return_bad_request(
        string field, string value, string reason, string expectedError, string expectedStatus)
    {
        _input = new InvalidFieldFromRequest(field, value, reason);
        _expectedError = expectedError;
        _expectedStatus = expectedStatus;

        this.Given(x => x.A_valid_muffin_request_with_an_invalid_field())
            .When(x => x.The_muffin_validation_request_is_sent())
            .Then(x => x.The_response_should_contain_the_expected_validation_error())
            .BDDfy();
    }

    #region Steps

    private async Task All_ingredients_are_retrieved_for_apple_cinnamon_muffins()
    {
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
    }

    private async Task The_muffins_are_prepared()
    {
        await _muffinSteps.Send();
    }

    private async Task The_response_should_contain_a_valid_batch_with_all_ingredients()
    {
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
    }

    private void The_cow_service_should_have_received_a_milk_request()
    {
        if (!Settings.RunAgainstExternalServiceUnderTest)
            _downstreamSteps.AssertCowServiceReceivedMilkRequest();
    }

    private async Task All_ingredients_are_retrieved_for_the_muffin_recipe()
    {
        await _milkSteps.Retrieve();
        _milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        _muffinSteps.Request.Milk = _milkSteps.MilkResponse.Milk;

        await _eggsSteps.Retrieve();
        _eggsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        _muffinSteps.Request.Eggs = _eggsSteps.EggsResponse.Eggs;

        _muffinSteps.Request.Flour = _recipe.Ingredients.Flour;
        _muffinSteps.Request.Apples = _recipe.Ingredients.Apples;
        _muffinSteps.Request.Cinnamon = _recipe.Ingredients.Cinnamon;
        _muffinSteps.Request.Baking = new TestBakingProfile
        {
            Temperature = _temperature,
            DurationMinutes = _durationMinutes,
            PanType = _panType
        };
        _muffinSteps.Request.Toppings = _recipe.Toppings?
            .Select(t => new TestMuffinTopping { Name = t.Name, Amount = t.Amount })
            .ToList();
    }

    private async Task The_response_should_match_the_expected_batch()
    {
        _muffinSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _muffinSteps.ParseResponse();
        _muffinSteps.Response!.Ingredients.Should().HaveCount(_expected.ExpectedIngredientCount);
        _muffinSteps.Response!.Toppings.Should().HaveCount(_expected.ExpectedToppingCount);
        var bakingTemperatureMatchesExpectation = _expected.HasBakingInfo
            ? _muffinSteps.Response!.BakingTemperature > 0
            : _muffinSteps.Response!.BakingTemperature == 0;
        bakingTemperatureMatchesExpectation.Should().BeTrue();
    }

    private Task A_valid_muffin_request_with_an_invalid_field()
    {
        return Task.CompletedTask;
    }

    private async Task The_muffin_validation_request_is_sent()
    {
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

        var requests = ValidationHelper.CreateValidationRequests(validBase, [_input]);
        var responses = await ValidationHelper.SendValidationRequests(
            Client, RequestId, Endpoints.Muffins, requests, [_input]);
        var actualResults = await ValidationHelper.ParseValidationResponses(responses);
        _actual = actualResults.Single();
    }

    private Task The_response_should_contain_the_expected_validation_error()
    {
        _actual!.ErrorMessage.Should().Contain(_expectedError);
        _actual!.ResponseStatus.Should().Be(_expectedStatus);
        return Task.CompletedTask;
    }

    #endregion
}
