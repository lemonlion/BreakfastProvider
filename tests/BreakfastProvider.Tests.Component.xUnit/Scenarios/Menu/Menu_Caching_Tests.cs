using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Menu;
using BreakfastProvider.Tests.Component.Shared.Constants;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Menu;

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

    [Fact]
    public async Task Menu_should_return_cached_results_on_subsequent_requests()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        // Given the menu has been requested and cached
        await Client.DeleteAsync(Endpoints.MenuCache);
        await _menuSteps.Retrieve();
        Track.That(() => _menuSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

        // And the supplier service is then made unavailable
        _secondMenuSteps.AddHeader(FakeScenarioHeaders.SupplierService, FakeScenarios.ServiceUnavailable);

        // When the menu is requested again
        await _secondMenuSteps.Retrieve();

        // Then the menu response should still return available items
        Track.That(() => _secondMenuSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _secondMenuSteps.ParseResponse();
        Track.That(() => _secondMenuSteps.Response!.Should().Contain(m => m.IsAvailable));
    }
}
