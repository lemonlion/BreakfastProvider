using System.Net;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Validation;
using BreakfastProvider.Tests.Component.Shared.Common.Waffles;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;
using BreakfastProvider.Tests.Component.Shared.Models.Waffles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Waffles;

[Binding]
public class WaffleCreationSteps(
    AppManager appManager,
    GetMilkSteps milkSteps,
    GetEggsSteps eggsSteps,
    GetFlourSteps flourSteps,
    PostWafflesSteps waffleSteps,
    DownstreamRequestSteps downstreamSteps)
{
    private readonly List<TestWaffleRequest> _validationRequests = [];
    private readonly List<HttpResponseMessage> _validationResponses = [];
    private readonly List<InvalidFieldFromRequest> _validationInputs = [];

    private ToppingRulesConfig? _toppingRules;
    private int MaxToppings => (_toppingRules ??=
        appManager.AppFactory.Services.GetRequiredService<IOptions<ToppingRulesConfig>>().Value).MaxToppingsPerItem;

    [Given("a valid waffle recipe with all ingredients")]
    public async Task GivenAValidWaffleRecipeWithAllIngredients()
    {
        await milkSteps.Retrieve();
        Track.That(() => milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        waffleSteps.Request.Milk = milkSteps.MilkResponse.Milk;

        await eggsSteps.Retrieve();
        Track.That(() => eggsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        waffleSteps.Request.Eggs = eggsSteps.EggsResponse.Eggs;

        await flourSteps.Retrieve();
        Track.That(() => flourSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        waffleSteps.Request.Flour = flourSteps.FlourResponse.Flour;

        waffleSteps.Request.Butter = IngredientDefaults.UnsaltedButter;
    }

    [Given(@"a valid waffle request with ""(.*)"" set to ""(.*)""")]
    public void GivenAValidWaffleRequestWithFieldSetToValue(string field, string value)
    {
        var validBase = new TestWaffleRequest
        {
            Milk = CowServiceDefaults.FreshMilk,
            Flour = IngredientDefaults.PlainFlour,
            Eggs = IngredientDefaults.FreeRangeEggs,
            Butter = IngredientDefaults.UnsaltedButter
        };

        var input = new InvalidFieldFromRequest(field, value, string.Empty);
        _validationInputs.Add(input);
        _validationRequests.AddRange(ValidationHelper.CreateValidationRequests(validBase, [input]));
    }

    [Given("the waffle request has more toppings than the configured limit")]
    public void GivenTheWaffleRequestHasMoreToppingsThanTheConfiguredLimit()
    {
        waffleSteps.Request.Toppings = Enumerable.Range(0, MaxToppings + 1)
            .Select(i => $"Topping_{i}")
            .ToList();
    }

    [When("the waffles are prepared")]
    public async Task WhenTheWafflesArePrepared()
    {
        await waffleSteps.Send();
    }

    [When("the invalid waffle request is submitted")]
    public async Task WhenTheInvalidWaffleRequestIsSubmitted()
    {
        _validationResponses.AddRange(
            await ValidationHelper.SendValidationRequests(
                appManager.Client, appManager.RequestId, Endpoints.Waffles, _validationRequests, _validationInputs));
    }

    [Then("the waffles response should contain a valid batch with all ingredients")]
    public async Task ThenTheWafflesResponseShouldContainAValidBatchWithAllIngredients()
    {
        Track.That(() => waffleSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await waffleSteps.ParseResponse();
        Track.That(() => waffleSteps.Response!.Ingredients.Should().Contain(milkSteps.MilkResponse.Milk));
        Track.That(() => waffleSteps.Response!.Ingredients.Should().Contain(eggsSteps.EggsResponse.Eggs));
        Track.That(() => waffleSteps.Response!.Ingredients.Should().Contain(flourSteps.FlourResponse.Flour));
        Track.That(() => waffleSteps.Response!.Ingredients.Should().Contain(IngredientDefaults.UnsaltedButter));
    }

    [Then("the waffles response should indicate too many toppings")]
    public async Task ThenTheWafflesResponseShouldIndicateTooManyToppings()
    {
        Track.That(() => waffleSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest));
        var waffleErrorResponseBody = await waffleSteps.ResponseMessage!.Content.ReadAsStringAsync();
        Track.That(() => waffleErrorResponseBody.Should().Contain(WaffleValidationMessages.MaxToppingsExceeded));
    }

    [Then(@"the waffle response should contain error ""(.*)"" with status ""(.*)""")]
    public async Task ThenTheWaffleResponseShouldContainErrorWithStatus(string errorMessage, string responseStatus)
    {
        var actualResults = await ValidationHelper.ParseValidationResponses(_validationResponses);
        Track.That(() => actualResults.Should().Contain(r => r.ErrorMessage.Contains(errorMessage)));
    }
}
