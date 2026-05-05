using System.Net;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.Toppings;
using BreakfastProvider.Tests.Component.Shared.Common.Validation;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Toppings;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Toppings;

[Binding]
public class ToppingsSteps(
    AppManager appManager,
    GetToppingsSteps getSteps,
    PostToppingsSteps postSteps,
    DeleteToppingSteps deleteSteps,
    PutToppingSteps putSteps)
{
    private Guid _toppingId;
    private readonly List<TestToppingRequest> _postValidationRequests = [];
    private readonly List<TestUpdateToppingRequest> _putValidationRequests = [];
    private readonly List<HttpResponseMessage> _validationResponses = [];
    private readonly List<InvalidFieldFromRequest> _validationInputs = [];

    // --- Management ---
    [Given("a valid topping request")]
    public void GivenAValidToppingRequest()
    {
        postSteps.Request = new TestToppingRequest
        {
            Name = ToppingDefaults.Strawberries,
            Category = ToppingDefaults.FruitCategory
        };
    }

    [When("the available toppings are requested")]
    public async Task WhenTheAvailableToppingsAreRequested() => await getSteps.Retrieve();

    [When("the new topping is submitted")]
    public async Task WhenTheNewToppingIsSubmitted() => await postSteps.Send();

    [Then("the toppings response should contain the default toppings")]
    public async Task ThenTheToppingsResponseShouldContainTheDefaultToppings()
    {
        getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await getSteps.ParseResponse();
        getSteps.Response.Should().HaveCount(ToppingDefaults.ExpectedToppingCount);
        getSteps.Response!.Should().Contain(t => t.Name == ToppingDefaults.Raspberries);
        getSteps.Response!.Should().Contain(t => t.Name == ToppingDefaults.Blueberries);
        getSteps.Response!.Should().Contain(t => t.Name == ToppingDefaults.MapleSyrup);
        getSteps.Response!.Should().Contain(t => t.Name == ToppingDefaults.WhippedCream);
        getSteps.Response!.Should().Contain(t => t.Name == ToppingDefaults.ChocolateChips);
    }

    [Then("the topping response should contain the created topping")]
    public async Task ThenTheToppingResponseShouldContainTheCreatedTopping()
    {
        postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await postSteps.ParseResponse();
        postSteps.Response!.Name.Should().Be(ToppingDefaults.Strawberries);
        postSteps.Response!.Category.Should().Be(ToppingDefaults.FruitCategory);
    }

    // --- Deletion ---
    [Given("a known topping exists")]
    public void GivenAKnownToppingExists() => _toppingId = ToppingDefaults.KnownRaspberryToppingId;

    [Given("a topping id that does not exist")]
    public void GivenAToppingIdThatDoesNotExist() => _toppingId = Guid.NewGuid();

    [When("the topping is deleted")]
    public async Task WhenTheToppingIsDeleted() => await deleteSteps.Send(_toppingId);

    [Then("the delete response should indicate success")]
    public void ThenTheDeleteResponseShouldIndicateSuccess()
        => deleteSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NoContent);

    [Then("the delete response should indicate not found")]
    public void ThenTheDeleteResponseShouldIndicateNotFound()
        => deleteSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);

    // --- Update ---
    [Given("a known blueberry topping exists")]
    public void GivenAKnownBlueberryToppingExists() => _toppingId = ToppingDefaults.KnownBlueberryToppingId;

    [Given("a valid update topping request")]
    public void GivenAValidUpdateToppingRequest()
    {
        putSteps.Request = new TestUpdateToppingRequest
        {
            Name = ToppingDefaults.Strawberries,
            Category = ToppingDefaults.FruitCategory
        };
    }

    [Given(@"a valid update topping request with ""(.*)"" set to ""(.*)""")]
    public void GivenAValidUpdateToppingRequestWithFieldSetToValue(string field, string value)
    {
        var validBase = new TestUpdateToppingRequest
        {
            Name = ToppingDefaults.Strawberries,
            Category = ToppingDefaults.FruitCategory
        };
        var input = new InvalidFieldFromRequest(field, value, string.Empty);
        _validationInputs.Add(input);
        _putValidationRequests.AddRange(ValidationHelper.CreateValidationRequests(validBase, [input]));
    }

    [When("the topping is updated")]
    public async Task WhenTheToppingIsUpdated() => await putSteps.Send(_toppingId);

    [When("the invalid update topping request is submitted")]
    public async Task WhenTheInvalidUpdateToppingRequestIsSubmitted()
    {
        _validationResponses.AddRange(
            await ValidationHelper.SendPutValidationRequests(
                appManager.Client, appManager.RequestId, $"{Endpoints.Toppings}/{_toppingId}", _putValidationRequests, _validationInputs));
    }

    [Then("the update response should contain the updated topping")]
    public async Task ThenTheUpdateResponseShouldContainTheUpdatedTopping()
    {
        putSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await putSteps.ParseResponse();
        putSteps.Response!.ToppingId.Should().Be(ToppingDefaults.KnownBlueberryToppingId);
        putSteps.Response!.Name.Should().Be(ToppingDefaults.Strawberries);
        putSteps.Response!.Category.Should().Be(ToppingDefaults.FruitCategory);
    }

    [Then("the update response should indicate not found")]
    public void ThenTheUpdateResponseShouldIndicateNotFound()
        => putSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);

    [Then(@"the update response should contain error ""(.*)"" with status ""(.*)""")]
    public async Task ThenTheUpdateResponseShouldContainErrorWithStatus(string errorMessage, string responseStatus)
    {
        var actualResults = await ValidationHelper.ParseValidationResponses(_validationResponses);
        actualResults.Should().Contain(r => r.ErrorMessage.Contains(errorMessage));
    }

    // --- XSS Validation ---
    [Given(@"a valid topping request with ""(.*)"" set to ""(.*)""")]
    public void GivenAValidToppingRequestWithFieldSetToValue(string field, string value)
    {
        var validBase = new TestToppingRequest
        {
            Name = ToppingDefaults.Strawberries,
            Category = ToppingDefaults.FruitCategory
        };
        var input = new InvalidFieldFromRequest(field, value, string.Empty);
        _validationInputs.Add(input);
        _postValidationRequests.AddRange(ValidationHelper.CreateValidationRequests(validBase, [input]));
    }

    [When("the invalid topping request is submitted")]
    public async Task WhenTheInvalidToppingRequestIsSubmitted()
    {
        _validationResponses.AddRange(
            await ValidationHelper.SendValidationRequests(
                appManager.Client, appManager.RequestId, Endpoints.Toppings, _postValidationRequests, _validationInputs));
    }

    [Then(@"the topping response should contain error ""(.*)"" with status ""(.*)""")]
    public async Task ThenTheToppingResponseShouldContainErrorWithStatus(string errorMessage, string responseStatus)
    {
        var actualResults = await ValidationHelper.ParseValidationResponses(_validationResponses);
        actualResults.Should().Contain(r => r.ErrorMessage.Contains(errorMessage));
    }

    // --- Feature Flag ---
    [Given("the raspberry topping feature flag is disabled")]
    public void GivenTheRaspberryToppingFeatureFlagIsDisabled()
    {
        appManager.CreateAppWithOverrides(new Dictionary<string, string?>
        {
            [$"{nameof(FeatureSwitchesConfig)}:{nameof(FeatureSwitchesConfig.IsRaspberryToppingEnabled)}"] = "false"
        });
    }

    [Given("the raspberry topping feature flag is enabled")]
    public void GivenTheRaspberryToppingFeatureFlagIsEnabled()
    {
        appManager.CreateAppWithOverrides(new Dictionary<string, string?>
        {
            [$"{nameof(FeatureSwitchesConfig)}:{nameof(FeatureSwitchesConfig.IsRaspberryToppingEnabled)}"] = "true"
        });
    }

    [When("toppings are requested")]
    public async Task WhenToppingsAreRequested() => await getSteps.Retrieve();

    [Then("the toppings response should not include raspberries")]
    public async Task ThenTheToppingsResponseShouldNotIncludeRaspberries()
    {
        getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await getSteps.ParseResponse();
        getSteps.Response!.Should().NotContain(t => t.Name == ToppingDefaults.Raspberries);
    }

    [Then("the toppings response should include raspberries")]
    public async Task ThenTheToppingsResponseShouldIncludeRaspberries()
    {
        getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await getSteps.ParseResponse();
        getSteps.Response!.Should().Contain(t => t.Name == ToppingDefaults.Raspberries);
    }
}
