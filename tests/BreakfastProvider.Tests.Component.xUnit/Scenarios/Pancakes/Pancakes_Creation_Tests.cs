using System.Net;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Common.Validation;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TestTrackingDiagrams.xUnit3;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Pancakes;

public class Pancakes_Creation_Tests : BaseFixture
{
    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostPancakesSteps _pancakeSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;

    public Pancakes_Creation_Tests()
    {
        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _pancakeSteps = Get<PostPancakesSteps>();
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

    private ToppingRulesConfig? _toppingRules;
    private ToppingRulesConfig ToppingRules => _toppingRules ??=
        AppFactory.Services.GetRequiredService<IOptions<ToppingRulesConfig>>().Value;
    private int MaxToppings => ToppingRules.MaxToppingsPerItem;

    [Fact]
    [HappyPath]
    public async Task Valid_pancake_request_with_all_ingredients_should_return_a_fresh_batch()
    {
        // Given a valid pancake recipe with all ingredients
        await _milkSteps.Retrieve();
        Track.That(() => _milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        _pancakeSteps.Request.Milk = _milkSteps.MilkResponse.Milk;

        await _eggsSteps.Retrieve();
        Track.That(() => _eggsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        _pancakeSteps.Request.Eggs = _eggsSteps.EggsResponse.Eggs;

        await _flourSteps.Retrieve();
        Track.That(() => _flourSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        _pancakeSteps.Request.Flour = _flourSteps.FlourResponse.Flour;

        // When the pancakes are prepared
        await _pancakeSteps.Send();

        // Then the response should contain a valid batch with all ingredients
        Track.That(() => _pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _pancakeSteps.ParseResponse();
        Track.That(() => _pancakeSteps.Response!.Ingredients.Should().Contain(_milkSteps.MilkResponse.Milk));
        Track.That(() => _pancakeSteps.Response!.Ingredients.Should().Contain(_eggsSteps.EggsResponse.Eggs));
        Track.That(() => _pancakeSteps.Response!.Ingredients.Should().Contain(_flourSteps.FlourResponse.Flour));

        // And the cow service should have received a milk request
        if (!Settings.RunAgainstExternalServiceUnderTest)
            _downstreamSteps.AssertCowServiceReceivedMilkRequest();
    }

    [Theory]
    [InlineData("Milk", "", "Milk is required", "'Milk' is required.", "Bad Request")]
    [InlineData("Flour", "", "Flour is required", "'Flour' is required.", "Bad Request")]
    [InlineData("Eggs", "", "Eggs is required", "'Eggs' is required.", "Bad Request")]
    [InlineData("Milk", "<script>alert</script>", "XSS in milk", "Milk contains potentially dangerous content.", "Bad Request")]
    [InlineData("Flour", "<img onerror=x>", "XSS in flour", "Flour contains potentially dangerous content.", "Bad Request")]
    [InlineData("Eggs", "javascript:void(0)", "XSS in eggs", "Eggs contains potentially dangerous content.", "Bad Request")]
    public async Task Pancake_request_with_invalid_ingredient_should_return_bad_request(
        string field, string value, string reason, string expectedError, string expectedStatus)
    {
        // Given valid pancake requests with an invalid field
        var validBase = new TestPancakeRequest
        {
            Milk = CowServiceDefaults.FreshMilk,
            Flour = IngredientDefaults.PlainFlour,
            Eggs = IngredientDefaults.FreeRangeEggs
        };

        var input = new InvalidFieldFromRequest(field, value, reason);
        var requests = ValidationHelper.CreateValidationRequests(validBase, new List<InvalidFieldFromRequest> { input });

        // When the invalid pancake requests are submitted
        var responses = await ValidationHelper.SendValidationRequests(
            Client, RequestId, Endpoints.Pancakes, requests, new List<InvalidFieldFromRequest> { input });

        // Then the responses should contain the validation error
        var actualResults = await ValidationHelper.ParseValidationResponses(responses);
        var actual = actualResults.Single();
        Track.That(() => actual.ErrorMessage.Should().Be(expectedError));
        Track.That(() => actual.ResponseStatus.Should().Be(expectedStatus));
    }

    [Fact(Skip = null)]
    public async Task Pancake_request_with_more_toppings_than_allowed_should_return_bad_request()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        // Given the max toppings per item is configured
        // And a valid pancake recipe with all ingredients
        await _milkSteps.Retrieve();
        _pancakeSteps.Request.Milk = _milkSteps.MilkResponse.Milk;
        await _eggsSteps.Retrieve();
        _pancakeSteps.Request.Eggs = _eggsSteps.EggsResponse.Eggs;
        await _flourSteps.Retrieve();
        _pancakeSteps.Request.Flour = _flourSteps.FlourResponse.Flour;

        // And the request has more toppings than the configured limit
        _pancakeSteps.Request.Toppings = Enumerable.Range(0, MaxToppings + 1)
            .Select(i => $"Topping_{i}")
            .ToList();

        // When the pancakes are prepared
        await _pancakeSteps.Send();

        // Then the response should indicate too many toppings
        Track.That(() => _pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest));
        var body = await _pancakeSteps.ResponseMessage!.Content.ReadAsStringAsync();
        Track.That(() => body.Should().Contain(PancakeValidationMessages.MaxToppingsExceeded));
    }
}
