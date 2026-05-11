using System.Net;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Validation;
using BreakfastProvider.Tests.Component.Shared.Common.Waffles;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;
using BreakfastProvider.Tests.Component.Shared.Models.Waffles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Waffles;

public class Waffles_Creation_Tests : BaseFixture
{
    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostWafflesSteps _waffleSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;
    private InvalidFieldFromRequest _input = null!;
    private VerifiableErrorResult? _actual;

    public Waffles_Creation_Tests()
    {
        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _waffleSteps = Get<PostWafflesSteps>();
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

    private ToppingRulesConfig? _toppingRules;
    private ToppingRulesConfig ToppingRules => _toppingRules ??=
        AppFactory.Services.GetRequiredService<IOptions<ToppingRulesConfig>>().Value;
    private int MaxToppings => ToppingRules.MaxToppingsPerItem;

    [Fact]
    [HappyPath]
    public void Valid_waffle_request_with_all_ingredients_should_return_a_fresh_batch()
    {
        this.Given(x => x.All_ingredients_are_retrieved_for_waffles())
            .When(x => x.The_waffles_are_prepared())
            .Then(x => x.The_response_should_contain_a_valid_batch_with_all_ingredients())
            .And(x => x.The_cow_service_should_have_received_a_milk_request())
            .BDDfy();
    }

    [Theory]
    [InlineData("Milk", "", "Milk is required", "'Milk' is required.", "Bad Request")]
    [InlineData("Flour", "", "Flour is required", "'Flour' is required.", "Bad Request")]
    [InlineData("Eggs", "", "Eggs is required", "'Eggs' is required.", "Bad Request")]
    [InlineData("Butter", "", "Butter is required", "'Butter' is required.", "Bad Request")]
    [InlineData("Milk", "<script>alert</script>", "XSS in milk", "Milk contains potentially dangerous content.", "Bad Request")]
    [InlineData("Butter", "<img onerror=x>", "XSS in butter", "Butter contains potentially dangerous content.", "Bad Request")]
    public void Waffle_request_with_invalid_ingredient_should_return_bad_request(
        string field, string value, string reason, string expectedError, string expectedStatus)
    {
        this.Given(x => x.A_waffle_request_with_an_invalid_FIELD(field, value, reason))
            .When(x => x.The_waffle_validation_request_is_sent())
            .Then(x => x.The_response_should_contain_error(expectedError))
            .And(x => x.The_response_status_should_be(expectedStatus))
            .BDDfy();
    }

    [Fact]
    public void Waffle_request_with_more_toppings_than_allowed_should_return_bad_request()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        this.Given(x => x.All_ingredients_are_retrieved_for_waffles())
            .And(x => x.The_request_has_more_toppings_than_the_configured_limit())
            .When(x => x.The_waffles_are_prepared())
            .Then(x => x.The_response_should_indicate_too_many_toppings())
            .BDDfy();
    }

    #region Steps

    private async Task All_ingredients_are_retrieved_for_waffles()
    {
        await _milkSteps.Retrieve();
        _milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        _waffleSteps.Request.Milk = _milkSteps.MilkResponse.Milk;

        await _eggsSteps.Retrieve();
        _eggsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        _waffleSteps.Request.Eggs = _eggsSteps.EggsResponse.Eggs;

        await _flourSteps.Retrieve();
        _flourSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        _waffleSteps.Request.Flour = _flourSteps.FlourResponse.Flour;

        _waffleSteps.Request.Butter = IngredientDefaults.UnsaltedButter;
    }

    private async Task The_waffles_are_prepared()
    {
        await _waffleSteps.Send();
    }

    private async Task The_response_should_contain_a_valid_batch_with_all_ingredients()
    {
        _waffleSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _waffleSteps.ParseResponse();
        _waffleSteps.Response!.Ingredients.Should().Contain(_milkSteps.MilkResponse.Milk);
        _waffleSteps.Response!.Ingredients.Should().Contain(_eggsSteps.EggsResponse.Eggs);
        _waffleSteps.Response!.Ingredients.Should().Contain(_flourSteps.FlourResponse.Flour);
        _waffleSteps.Response!.Ingredients.Should().Contain(IngredientDefaults.UnsaltedButter);
    }

    private void The_cow_service_should_have_received_a_milk_request()
    {
        if (!Settings.RunAgainstExternalServiceUnderTest)
            _downstreamSteps.AssertCowServiceReceivedMilkRequest();
    }

    private void The_request_has_more_toppings_than_the_configured_limit()
    {
        _waffleSteps.Request.Toppings = Enumerable.Range(0, MaxToppings + 1)
            .Select(i => $"Topping_{i}")
            .ToList();
    }

    private async Task The_response_should_indicate_too_many_toppings()
    {
        _waffleSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await _waffleSteps.ResponseMessage!.Content.ReadAsStringAsync();
        body.Should().Contain(WaffleValidationMessages.MaxToppingsExceeded);
    }

    private void A_waffle_request_with_an_invalid_FIELD(string field, string value, string reason)
    {
        _input = new InvalidFieldFromRequest(field, value, reason);
    }

    private async Task The_waffle_validation_request_is_sent()
    {
        var validBase = new TestWaffleRequest
        {
            Milk = CowServiceDefaults.FreshMilk,
            Flour = IngredientDefaults.PlainFlour,
            Eggs = IngredientDefaults.FreeRangeEggs,
            Butter = IngredientDefaults.UnsaltedButter
        };

        var requests = ValidationHelper.CreateValidationRequests(validBase, [_input]);
        var responses = await ValidationHelper.SendValidationRequests(
            Client, RequestId, Endpoints.Waffles, requests, [_input]);
        var actualResults = await ValidationHelper.ParseValidationResponses(responses);
        _actual = actualResults.Single();
    }

    private void The_response_should_contain_error(string expectedError)
    {
        _actual!.ErrorMessage.Should().Be(expectedError);
    }

    private void The_response_status_should_be(string expectedStatus)
    {
        _actual!.ResponseStatus.Should().Be(expectedStatus);
    }

    #endregion
}
