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
using TestTrackingDiagrams.xUnit3;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Waffles;

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

    [Fact]
    [HappyPath]
    public async Task Valid_waffle_request_with_all_ingredients_should_return_a_fresh_batch()
    {
        // Given a valid waffle recipe with all ingredients
        await _milkSteps.Retrieve();
        Track.That(() => _milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        _waffleSteps.Request.Milk = _milkSteps.MilkResponse.Milk;

        await _eggsSteps.Retrieve();
        Track.That(() => _eggsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        _waffleSteps.Request.Eggs = _eggsSteps.EggsResponse.Eggs;

        await _flourSteps.Retrieve();
        Track.That(() => _flourSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        _waffleSteps.Request.Flour = _flourSteps.FlourResponse.Flour;

        _waffleSteps.Request.Butter = IngredientDefaults.UnsaltedButter;

        // When the waffles are prepared
        await _waffleSteps.Send();

        // Then the response should contain a valid batch with all ingredients
        Track.That(() => _waffleSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _waffleSteps.ParseResponse();
        Track.That(() => _waffleSteps.Response!.Ingredients.Should().Contain(_milkSteps.MilkResponse.Milk));
        Track.That(() => _waffleSteps.Response!.Ingredients.Should().Contain(_eggsSteps.EggsResponse.Eggs));
        Track.That(() => _waffleSteps.Response!.Ingredients.Should().Contain(_flourSteps.FlourResponse.Flour));
        Track.That(() => _waffleSteps.Response!.Ingredients.Should().Contain(IngredientDefaults.UnsaltedButter));

        // And the cow service should have received a milk request
        if (!Settings.RunAgainstExternalServiceUnderTest)
            _downstreamSteps.AssertCowServiceReceivedMilkRequest();
    }

    [Theory]
    [InlineData("Milk", "", "Milk is required", "'Milk' is required.", "Bad Request")]
    [InlineData("Flour", "", "Flour is required", "'Flour' is required.", "Bad Request")]
    [InlineData("Eggs", "", "Eggs is required", "'Eggs' is required.", "Bad Request")]
    [InlineData("Butter", "", "Butter is required", "'Butter' is required.", "Bad Request")]
    [InlineData("Milk", "<script>alert</script>", "XSS in milk", "Milk contains potentially dangerous content.", "Bad Request")]
    [InlineData("Butter", "<img onerror=x>", "XSS in butter", "Butter contains potentially dangerous content.", "Bad Request")]
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
        Track.That(() => actual.ErrorMessage.Should().Be(expectedError));
        Track.That(() => actual.ResponseStatus.Should().Be(expectedStatus));
    }

    [Fact]
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
        Track.That(() => _waffleSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest));
        var body = await _waffleSteps.ResponseMessage!.Content.ReadAsStringAsync();
        Track.That(() => body.Should().Contain(WaffleValidationMessages.MaxToppingsExceeded));
    }
}
