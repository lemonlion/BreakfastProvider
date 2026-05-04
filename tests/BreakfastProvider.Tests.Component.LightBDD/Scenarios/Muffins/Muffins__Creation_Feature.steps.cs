#pragma warning disable CS1998
using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Muffins;
using BreakfastProvider.Tests.Component.Shared.Common.Validation;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Muffins;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Muffins;

public partial class Muffins__Creation_Feature : BaseFixture
{
    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostMuffinsSteps _muffinSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;

    private readonly List<HttpResponseMessage> _validationResponses = [];

    public Muffins__Creation_Feature()
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
        Track.That(() => _milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        _muffinSteps.Request.Milk = _milkSteps.MilkResponse.Milk;

        await _eggsSteps.Retrieve();
        Track.That(() => _eggsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        _muffinSteps.Request.Eggs = _eggsSteps.EggsResponse.Eggs;

        await _flourSteps.Retrieve();
        Track.That(() => _flourSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
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

    private async Task A_muffin_recipe_with_ingredients_and_baking_profile(string recipeName, MuffinRecipeTestData recipe)
    {
        await _milkSteps.Retrieve();
        Track.That(() => _milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        _muffinSteps.Request.Milk = _milkSteps.MilkResponse.Milk;

        await _eggsSteps.Retrieve();
        Track.That(() => _eggsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        _muffinSteps.Request.Eggs = _eggsSteps.EggsResponse.Eggs;

        _muffinSteps.Request.Flour = recipe.Ingredients.Flour;
        _muffinSteps.Request.Apples = recipe.Ingredients.Apples;
        _muffinSteps.Request.Cinnamon = recipe.Ingredients.Cinnamon;
        _muffinSteps.Request.Baking = new TestBakingProfile
        {
            Temperature = recipe.Baking.Temperature,
            DurationMinutes = recipe.Baking.DurationMinutes,
            PanType = recipe.Baking.PanType
        };
        _muffinSteps.Request.Toppings = recipe.Toppings
            .Select(t => new TestMuffinTopping { Name = t.Name, Amount = t.Amount })
            .ToList();
    }

    private async Task A_valid_muffin_request_with_an_invalid_field(string field, string value)
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
        Track.That(() => _muffinSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _muffinSteps.ParseResponse();
        Track.That(() => _muffinSteps.Response!.Ingredients.Should().Contain(_milkSteps.MilkResponse.Milk));
        Track.That(() => _muffinSteps.Response!.Ingredients.Should().Contain(_eggsSteps.EggsResponse.Eggs));
        Track.That(() => _muffinSteps.Response!.Ingredients.Should().Contain(_flourSteps.FlourResponse.Flour));
        Track.That(() => _muffinSteps.Response!.Ingredients.Should().Contain(MuffinDefaults.GrannySmithApples));
        Track.That(() => _muffinSteps.Response!.Ingredients.Should().Contain(MuffinDefaults.CeylonCinnamon));
        Track.That(() => _muffinSteps.Response!.Toppings.Should().HaveCount(1));
        Track.That(() => _muffinSteps.Response!.BakingTemperature.Should().Be(MuffinDefaults.DefaultTemperature));
        Track.That(() => _muffinSteps.Response!.BakingDuration.Should().Be(MuffinDefaults.DefaultDuration));
    }

    private async Task The_muffin_batch_should_match_the_expected_outcome(MuffinBatchExpectation expected)
    {
        Track.That(() => _muffinSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _muffinSteps.ParseResponse();
        Track.That(() => _muffinSteps.Response!.Ingredients.Should().HaveCount(expected.ExpectedIngredientCount));
        Track.That(() => _muffinSteps.Response!.Toppings.Should().HaveCount(expected.ExpectedToppingCount));
        Track.That(() => (expected.HasBakingInfo
            ? _muffinSteps.Response!.BakingTemperature > 0
            : _muffinSteps.Response!.BakingTemperature == 0).Should().BeTrue());
    }

    private async Task The_muffin_response_should_contain_the_validation_error(string expectedError, string expectedStatus)
    {
        var responseBody = await _muffinSteps.ResponseMessage!.Content.ReadAsStringAsync();
        Track.That(() => responseBody.Should().Contain(expectedError));
        Track.That(() => _muffinSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest));
    }

    private async Task The_cow_service_should_have_received_a_milk_request()
    {
        if (!Settings.RunAgainstExternalServiceUnderTest)
            _downstreamSteps.AssertCowServiceReceivedMilkRequest();
    }

    #endregion
}
