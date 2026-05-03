using System.Net;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Menu;
using BreakfastProvider.Tests.Component.Shared.Constants;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Menu;

[Binding]
public class MenuSteps(
    AppManager appManager,
    GetMenuSteps menuSteps,
    DownstreamRequestSteps downstreamSteps)
{
    private GetMenuSteps? _secondMenuSteps;

    [BeforeScenario("MenuAvailability", Order = 50)]
    public void SetupMenuAvailabilityApp()
    {
        appManager.CreateAppWithOverrides(new Dictionary<string, string?> { ["TestIsolation"] = "MenuAvailability" });
    }

    [BeforeScenario("MenuCaching", Order = 50)]
    public void SetupMenuCachingApp()
    {
        appManager.CreateAppWithOverrides(new Dictionary<string, string?> { ["TestIsolation"] = "MenuCaching" });
    }

    [BeforeScenario("MenuDownstreamFailure", Order = 50)]
    public void SetupMenuDownstreamFailureApp()
    {
        appManager.CreateAppWithOverrides(new Dictionary<string, string?> { ["TestIsolation"] = "MenuDownstreamFailure" });
    }

    [When("the menu is requested")]
    public async Task WhenTheMenuIsRequested() => await menuSteps.Retrieve();

    [Then("the menu response should contain all menu items")]
    public async Task ThenTheMenuResponseShouldContainAllMenuItems()
    {
        Track.That(() => menuSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await menuSteps.ParseResponse();
        Track.That(() => menuSteps.Response!.Should().Contain(m => m.Name == MenuDefaults.ClassicPancakes));
        Track.That(() => menuSteps.Response!.Should().Contain(m => m.Name == MenuDefaults.BelgianWaffles));
        Track.That(() => menuSteps.Response!.Should().Contain(m => m.Name == MenuDefaults.GoatMilkPancakes));
    }

    [Then("the menu items should be in alphabetical order")]
    public async Task ThenTheMenuItemsShouldBeInAlphabeticalOrder()
    {
        await menuSteps.ParseResponse();
        Track.That(() => menuSteps.Response!.Should().BeInAscendingOrder(m => m.Name));
    }

    [Then("the supplier service should have received an availability request")]
    public void ThenTheSupplierServiceShouldHaveReceivedAnAvailabilityRequest()
    {
        if (AppManager.Settings.RunAgainstExternalServiceUnderTest) return;
        downstreamSteps.AssertSupplierServiceReceivedAvailabilityRequest();
    }

    // --- Caching ---
    [Given("the menu has been requested and cached")]
    public async Task GivenTheMenuHasBeenRequestedAndCached()
    {
        await appManager.Client.DeleteAsync(Endpoints.MenuCache);
        await menuSteps.Retrieve();
        Track.That(() => menuSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
    }

    [Given("the supplier service is then made unavailable")]
    public void GivenTheSupplierServiceIsThenMadeUnavailable()
    {
        _secondMenuSteps = menuSteps;
        _secondMenuSteps.AddHeader(FakeScenarioHeaders.SupplierService, FakeScenarios.ServiceUnavailable);
    }

    [When("the menu is requested again")]
    public async Task WhenTheMenuIsRequestedAgain() => await (_secondMenuSteps ?? menuSteps).Retrieve();

    [Then("the menu response should still return available items")]
    public async Task ThenTheMenuResponseShouldStillReturnAvailableItems()
    {
        var steps = _secondMenuSteps ?? menuSteps;
        Track.That(() => steps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await steps.ParseResponse();
        Track.That(() => steps.Response!.Should().Contain(m => m.IsAvailable));
    }

    // --- Downstream Failure ---
    [Given("the supplier service will return service unavailable")]
    public async Task GivenTheSupplierServiceWillReturnServiceUnavailable()
    {
        await appManager.Client.DeleteAsync(Endpoints.MenuCache);
        menuSteps.AddHeader(FakeScenarioHeaders.SupplierService, FakeScenarios.ServiceUnavailable);
    }

    [Then("the menu response should mark all items as unavailable")]
    public async Task ThenTheMenuResponseShouldMarkAllItemsAsUnavailable()
    {
        Track.That(() => menuSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await menuSteps.ParseResponse();
        Track.That(() => menuSteps.Response!.Should().OnlyContain(m => m.IsAvailable == false));
    }
}
