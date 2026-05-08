using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Menu;
using BreakfastProvider.Tests.Component.Shared.Constants;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.Menu;

public class Menu_Caching_Tests : BaseFixture
{
    private readonly GetMenuSteps _menuSteps;
    private readonly GetMenuSteps _secondMenuSteps;

    public Menu_Caching_Tests() : base(delayAppCreation: true)
    {
        CreateAppAndClient(new Dictionary<string, string?> { ["TestIsolation"] = "MenuCaching" });
        _menuSteps = Get<GetMenuSteps>();
        _secondMenuSteps = Get<GetMenuSteps>();
    }

    [Test]
    public async Task Menu_should_return_cached_results_on_subsequent_requests()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        // Given the menu has been requested and cached
        await Client.DeleteAsync(Endpoints.MenuCache);
        await _menuSteps.Retrieve();
        await _menuSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);

        // And the supplier service is then made unavailable
        _secondMenuSteps.AddHeader(FakeScenarioHeaders.SupplierService, FakeScenarios.ServiceUnavailable);

        // When the menu is requested again
        await _secondMenuSteps.Retrieve();

        // Then the menu response should still return available items
        await _secondMenuSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _secondMenuSteps.ParseResponse();
        await _secondMenuSteps.Response!.Should().Contain(m => m.IsAvailable);
    }
}
