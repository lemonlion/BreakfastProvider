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
using TestTrackingDiagrams.TUnit;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.Waffles;

#pragma warning disable CS1998
public class Waffles_Creation_Tests : BaseFixture
{
    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostWafflesSteps _waffleSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;

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

    [Test]
    [HappyPath]
    public async Task Valid_waffle_request_with_all_ingredients_should_return_a_fresh_batch()
    {
        // Given a valid waffle recipe with all ingredients
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

        // When the waffles are prepared
        await _waffleSteps.Send();

        // Then the response should contain a valid batch with all ingredients
        _waffleSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _waffleSteps.ParseResponse();
        _waffleSteps.Response!.Ingredients.Should().Contain(_milkSteps.MilkResponse.Milk);
        _waffleSteps.Response!.Ingredients.Should().Contain(_eggsSteps.EggsResponse.Eggs);
        _waffleSteps.Response!.Ingredients.Should().Contain(_flourSteps.FlourResponse.Flour);
        _waffleSteps.Response!.Ingredients.Should().Contain(IngredientDefaults.UnsaltedButter);

        // And the cow service should have received a milk request
        if (!Settings.RunAgainstExternalServiceUnderTest)
            _downstreamSteps.AssertCowServiceReceivedMilkRequest();
    }

    [Test]
    [Arguments("Milk", "", "Milk is required", "'Milk' is required.", "Bad Request")]
    [Arguments("Flour", "", "Flour is required", "'Flour' is required.", "Bad Request")]
    [Arguments("Eggs", "", "Eggs is required", "'Eggs' is required.", "Bad Request")]
    [Arguments("Butter", "", "Butter is required", "'Butter' is required.", "Bad Request")]
    [Arguments("Milk", "<script>alert</script>", "XSS in milk", "Milk contains potentially dangerous content.", "Bad Request")]
    [Arguments("Butter", "<img onerror=x>", "XSS in butter", "Butter contains potentially dangerous content.", "Bad Request")]
    public async Task Waffle_request_with_invalid_ingredient_should_return_bad_request(
        string field, string value, string reason, string expectedError, string expectedStatus)
    {
        // Given valid waffle requests with an invalid field
        var validBase = new TestWaffleRequest
        {
            Milk = CowServiceDefaults.FreshMilk,
            Flour = IngredientDefaults.PlainFlour,
            Eggs = IngredientDefaults.FreeRangeEggs,
            Butter = IngredientDefaults.UnsaltedButter
        };

        var input = new InvalidFieldFromRequest(field, value, reason);
        var requests = ValidationHelper.CreateValidationRequests(validBase, new List<InvalidFieldFromRequest> { input });

        // When the invalid waffle requests are submitted
        var responses = await ValidationHelper.SendValidationRequests(
            Client, RequestId, Endpoints.Waffles, requests, new List<InvalidFieldFromRequest> { input });

        // Then the responses should contain the validation error
        var actualResults = await ValidationHelper.ParseValidationResponses(responses);
        var actual = actualResults.Single();
        actual.ErrorMessage.Should().Be(expectedError);
        actual.ResponseStatus.Should().Be(expectedStatus);
    }

    [Test]
    public async Task Waffle_request_with_more_toppings_than_allowed_should_return_bad_request()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        // Given a valid waffle recipe with all ingredients
        await _milkSteps.Retrieve();
        _waffleSteps.Request.Milk = _milkSteps.MilkResponse.Milk;
        await _eggsSteps.Retrieve();
        _waffleSteps.Request.Eggs = _eggsSteps.EggsResponse.Eggs;
        await _flourSteps.Retrieve();
        _waffleSteps.Request.Flour = _flourSteps.FlourResponse.Flour;
        _waffleSteps.Request.Butter = IngredientDefaults.UnsaltedButter;

        // And the request has more toppings than the configured limit
        _waffleSteps.Request.Toppings = Enumerable.Range(0, MaxToppings + 1)
            .Select(i => $"Topping_{i}")
            .ToList();

        // When the waffles are prepared
        await _waffleSteps.Send();

        // Then the response should indicate too many toppings
        _waffleSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await _waffleSteps.ResponseMessage!.Content.ReadAsStringAsync();
        body.Should().Contain(WaffleValidationMessages.MaxToppingsExceeded);
    }
}
