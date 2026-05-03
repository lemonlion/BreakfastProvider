using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Menu;
using BreakfastProvider.Tests.Component.Shared.Constants;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Menu;

public class Menu_Downstream_Failure_Tests : BaseFixture
{
    private readonly GetMenuSteps _menuSteps;

    public Menu_Downstream_Failure_Tests() : base(delayAppCreation: true)
    {
        CreateAppAndClient(new Dictionary<string, string?> { ["TestIsolation"] = "MenuDownstreamFailure" });
        _menuSteps = Get<GetMenuSteps>();
    }

    [Fact]
    public async Task Requesting_menu_when_supplier_service_unavailable_should_mark_items_as_unavailable()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        // Given the supplier service will return service unavailable
        await Client.DeleteAsync(Endpoints.MenuCache);
        _menuSteps.AddHeader(FakeScenarioHeaders.SupplierService, FakeScenarios.ServiceUnavailable);

        // When the menu is requested
        await _menuSteps.Retrieve();

        // Then the menu response should mark all items as unavailable
        Track.That(() => _menuSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _menuSteps.ParseResponse();
        Track.That(() => _menuSteps.Response!.Should().OnlyContain(m => m.IsAvailable == false));
    }
}
