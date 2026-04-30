using System.Net;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Constants;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Ingredients;

[Binding]
public class IngredientsSteps(
    AppManager appManager,
    GetMilkSteps milkSteps,
    GetGoatMilkSteps goatMilkSteps,
    DownstreamRequestSteps downstreamSteps)
{
    // --- Milk Downstream Failure ---
    [Given("the cow service will return service unavailable")]
    public void GivenTheCowServiceWillReturnServiceUnavailable()
        => milkSteps.AddHeader(FakeScenarioHeaders.CowService, FakeScenarios.ServiceUnavailable);

    [Given("the cow service will return a timeout")]
    public void GivenTheCowServiceWillReturnATimeout()
        => milkSteps.AddHeader(FakeScenarioHeaders.CowService, FakeScenarios.Timeout);

    [Given("the cow service will return an invalid response")]
    public void GivenTheCowServiceWillReturnAnInvalidResponse()
        => milkSteps.AddHeader(FakeScenarioHeaders.CowService, FakeScenarios.InvalidResponse);

    [When("milk is requested")]
    public async Task WhenMilkIsRequested() => await milkSteps.Retrieve();

    [Then("the milk response should indicate a bad gateway")]
    public async Task ThenTheMilkResponseShouldIndicateABadGateway()
    {
        milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        var content = await milkSteps.ResponseMessage!.Content.ReadAsStringAsync();
        content.Should().Contain(DownstreamErrorMessages.CowServiceUnavailableTitle);
    }

    // --- Goat Milk Sourcing ---
    [When("goat milk is requested")]
    public async Task WhenGoatMilkIsRequested() => await goatMilkSteps.Retrieve();

    [Then("the goat milk response should contain fresh goat milk")]
    public void ThenTheGoatMilkResponseShouldContainFreshGoatMilk()
    {
        goatMilkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        goatMilkSteps.GoatMilkResponse.GoatMilk.Should().Be(GoatServiceDefaults.FreshGoatMilk);
    }

    [Then("the goat service should have received a goat milk request")]
    public void ThenTheGoatServiceShouldHaveReceivedAGoatMilkRequest()
    {
        if (AppManager.Settings.RunAgainstExternalServiceUnderTest) return;
        downstreamSteps.AssertGoatServiceReceivedGoatMilkRequest();
    }

    // --- Goat Milk Feature Flag ---
    [Given("the goat milk feature flag is disabled")]
    public void GivenTheGoatMilkFeatureFlagIsDisabled()
    {
        appManager.CreateAppWithOverrides(new Dictionary<string, string?>
        {
            [$"{nameof(FeatureSwitchesConfig)}:{nameof(FeatureSwitchesConfig.IsGoatMilkEnabled)}"] = "false"
        });
    }

    [Given("the goat milk feature flag is enabled")]
    public void GivenTheGoatMilkFeatureFlagIsEnabled()
    {
        appManager.CreateAppWithOverrides(new Dictionary<string, string?>
        {
            [$"{nameof(FeatureSwitchesConfig)}:{nameof(FeatureSwitchesConfig.IsGoatMilkEnabled)}"] = "true"
        });
    }

    [Then("the goat milk response should indicate feature disabled")]
    public async Task ThenTheGoatMilkResponseShouldIndicateFeatureDisabled()
    {
        goatMilkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await goatMilkSteps.ResponseMessage!.Content.ReadAsStringAsync();
        content.Should().Contain(DownstreamErrorMessages.FeatureDisabled);
    }

    [Then("the goat milk response should contain fresh goat milk when enabled")]
    public void ThenTheGoatMilkResponseShouldContainFreshGoatMilkWhenEnabled()
    {
        goatMilkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        goatMilkSteps.GoatMilkResponse.GoatMilk.Should().Be(GoatServiceDefaults.FreshGoatMilk);
    }

    // --- Goat Milk Downstream Failure ---
    [Given("the goat service will return service unavailable")]
    public void GivenTheGoatServiceWillReturnServiceUnavailable()
        => goatMilkSteps.AddHeader(FakeScenarioHeaders.GoatService, FakeScenarios.ServiceUnavailable);

    [Given("the goat service will return an invalid response")]
    public void GivenTheGoatServiceWillReturnAnInvalidResponse()
        => goatMilkSteps.AddHeader(FakeScenarioHeaders.GoatService, FakeScenarios.InvalidResponse);

    [Then("the goat milk response should indicate a bad gateway")]
    public async Task ThenTheGoatMilkResponseShouldIndicateABadGateway()
    {
        goatMilkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        var content = await goatMilkSteps.ResponseMessage!.Content.ReadAsStringAsync();
        content.Should().Contain(DownstreamErrorMessages.GoatServiceUnavailableTitle);
    }

}
