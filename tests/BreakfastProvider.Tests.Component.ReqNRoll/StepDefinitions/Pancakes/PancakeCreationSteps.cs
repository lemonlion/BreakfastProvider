using System.Net;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Common.Validation;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Pancakes;

[Binding]
public class PancakeCreationSteps(
    AppManager appManager,
    GetMilkSteps milkSteps,
    GetEggsSteps eggsSteps,
    GetFlourSteps flourSteps,
    PostPancakesSteps pancakeSteps)
{
    private readonly List<TestPancakeRequest> _validationRequests = [];
    private readonly List<HttpResponseMessage> _validationResponses = [];
    private readonly List<InvalidFieldFromRequest> _validationInputs = [];

    private ToppingRulesConfig? _toppingRules;
    private int MaxToppings => (_toppingRules ??=
        appManager.AppFactory.Services.GetRequiredService<IOptions<ToppingRulesConfig>>().Value).MaxToppingsPerItem;

    [Given("a valid pancake recipe with all ingredients")]
    public async Task GivenAValidPancakeRecipeWithAllIngredients()
    {
        await milkSteps.Retrieve();
        Track.That(() => milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        pancakeSteps.Request.Milk = milkSteps.MilkResponse.Milk;

        await eggsSteps.Retrieve();
        Track.That(() => eggsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        pancakeSteps.Request.Eggs = eggsSteps.EggsResponse.Eggs;

        await flourSteps.Retrieve();
        Track.That(() => flourSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        pancakeSteps.Request.Flour = flourSteps.FlourResponse.Flour;
    }

    [Given(@"a valid pancake request with ""(.*)"" set to ""(.*)""")]
    public void GivenAValidPancakeRequestWithFieldSetToValue(string field, string value)
    {
        var validBase = new TestPancakeRequest
        {
            Milk = CowServiceDefaults.FreshMilk,
            Flour = IngredientDefaults.PlainFlour,
            Eggs = IngredientDefaults.FreeRangeEggs
        };

        var input = new InvalidFieldFromRequest(field, value, string.Empty);
        _validationInputs.Add(input);
        _validationRequests.AddRange(ValidationHelper.CreateValidationRequests(validBase, [input]));
    }

    [Given("the max toppings per item is the configured limit")]
    public void GivenTheMaxToppingsPerItemIsTheConfiguredLimit()
    {
        // Informational — config value is read from appsettings
    }

    [Given("the request has more toppings than the configured limit")]
    public void GivenTheRequestHasMoreToppingsThanTheConfiguredLimit()
    {
        pancakeSteps.Request.Toppings = Enumerable.Range(0, MaxToppings + 1)
            .Select(i => $"Topping_{i}")
            .ToList();
    }

    [When("the pancakes are prepared")]
    public async Task WhenThePancakesArePrepared()
    {
        await pancakeSteps.Send();
    }

    [When("the invalid pancake request is submitted")]
    public async Task WhenTheInvalidPancakeRequestIsSubmitted()
    {
        _validationResponses.AddRange(
            await ValidationHelper.SendValidationRequests(
                appManager.Client, appManager.RequestId, Endpoints.Pancakes, _validationRequests, _validationInputs));
    }

    [Then("the pancakes response should contain a valid batch with all ingredients")]
    public async Task ThenThePancakesResponseShouldContainAValidBatchWithAllIngredients()
    {
        Track.That(() => pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await pancakeSteps.ParseResponse();
        Track.That(() => pancakeSteps.Response!.Ingredients.Should().Contain(milkSteps.MilkResponse.Milk));
        Track.That(() => pancakeSteps.Response!.Ingredients.Should().Contain(eggsSteps.EggsResponse.Eggs));
        Track.That(() => pancakeSteps.Response!.Ingredients.Should().Contain(flourSteps.FlourResponse.Flour));
    }

    [Then(@"the response should contain error ""(.*)"" with status ""(.*)""")]
    public async Task ThenTheResponseShouldContainErrorWithStatus(string errorMessage, string responseStatus)
    {
        var actualResults = await ValidationHelper.ParseValidationResponses(_validationResponses);
        Track.That(() => actualResults.Should().Contain(r => r.ErrorMessage.Contains(errorMessage)));
    }

    [Then("the pancakes response should indicate too many toppings")]
    public async Task ThenThePancakesResponseShouldIndicateTooManyToppings()
    {
        Track.That(() => pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest));
        var pancakeErrorResponseBody = await pancakeSteps.ResponseMessage!.Content.ReadAsStringAsync();
        Track.That(() => pancakeErrorResponseBody.Should().Contain(PancakeValidationMessages.MaxToppingsExceeded));
    }
}
