#pragma warning disable CS1998
using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.AppleCinnamonMuffins;
using BreakfastProvider.Tests.Component.Shared.Common.Validation;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.AppleCinnamonMuffins;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;
using Kronikol.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.AppleCinnamonMuffins;

public partial class Apple_Cinnamon_Muffins__Creation_Feature : BaseFixture
{
    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostMuffinsSteps _muffinSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;

    private readonly List<HttpResponseMessage> _validationResponses = [];

    public Apple_Cinnamon_Muffins__Creation_Feature()
    {
        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _muffinSteps = Get<PostMuffinsSteps>();
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

    #region Given

    private async Task A_valid_muffin_recipe_with_all_ingredients()
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
        _muffinSteps.Request.Toppings =
        [
            new TestMuffinTopping { Name = "Streusel", Amount = "Light" }
        ];
    }

    private async Task A_NAME_muffin_recipe_at_TEMPERATURE_degrees_for_DURATION_minutes_in_PAN(string name, MuffinRecipeTestData recipeData, int temperature, int duration, string pan)
    {
        await _milkSteps.Retrieve();
        _milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        _muffinSteps.Request.Milk = _milkSteps.MilkResponse.Milk;

        await _eggsSteps.Retrieve();
        _eggsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        _muffinSteps.Request.Eggs = _eggsSteps.EggsResponse.Eggs;

        _muffinSteps.Request.Flour = recipeData.Ingredients.Flour;
        _muffinSteps.Request.Apples = recipeData.Ingredients.Apples;
        _muffinSteps.Request.Cinnamon = recipeData.Ingredients.Cinnamon;
        _muffinSteps.Request.Baking = new TestBakingProfile
        {
            Temperature = temperature,
            DurationMinutes = duration,
            PanType = pan
        };
        _muffinSteps.Request.Toppings = recipeData.Toppings?
            .Select(t => new TestMuffinTopping { Name = t.Name, Amount = t.Amount })
            .ToList();
    }

    private async Task A_valid_muffin_request_with_an_invalid_field_set_to_VALUE(string field, string value)
    {
        _muffinSteps.Request = new TestMuffinRequest
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

        var input = new InvalidFieldFromRequest(field, value, string.Empty);
        var requests = ValidationHelper.CreateValidationRequests(_muffinSteps.Request, [input]);
        _muffinSteps.Request = requests.Single();
    }

    #endregion

    #region When

    private async Task The_muffins_are_prepared()
        => await _muffinSteps.Send();

    private async Task The_invalid_muffin_request_is_submitted()
        => await _muffinSteps.Send();

    #endregion

    #region Then

    private async Task The_muffin_response_should_contain_a_valid_batch_with_all_ingredients()
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

    private async Task The_muffin_batch_should_match_the_expected_outcome(MuffinBatchExpectation expected)
    {
        _muffinSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _muffinSteps.ParseResponse();
        _muffinSteps.Response!.Ingredients.Should().HaveCount(expected.ExpectedIngredientCount);
        _muffinSteps.Response!.Toppings.Should().HaveCount(expected.ExpectedToppingCount);
        var bakingTemperatureMatchesExpectation = expected.HasBakingInfo
            ? _muffinSteps.Response!.BakingTemperature > 0
            : _muffinSteps.Response!.BakingTemperature == 0;
        bakingTemperatureMatchesExpectation.Should().BeTrue();
    }

    private async Task The_muffin_response_should_contain_ERROR_with_STATUS(string error, string status)
    {
        var responseBody = await _muffinSteps.ResponseMessage!.Content.ReadAsStringAsync();
        responseBody.Should().Contain(error);
        _muffinSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task The_cow_service_should_have_received_a_milk_request()
    {
        if (!Settings.RunAgainstExternalServiceUnderTest)
            _downstreamSteps.AssertCowServiceReceivedMilkRequest();
    }

    #endregion
}
