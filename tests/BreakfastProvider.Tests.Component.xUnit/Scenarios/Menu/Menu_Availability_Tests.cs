using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Menu;
using BreakfastProvider.Tests.Component.Shared.Constants;
using TestTrackingDiagrams.xUnit3;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Menu;

public class Menu_Availability_Tests : BaseFixture
{
    private readonly GetMenuSteps _menuSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;

    public Menu_Availability_Tests() : base(delayAppCreation: true)
    {
        CreateAppAndClient(new Dictionary<string, string?> { ["TestIsolation"] = "MenuAvailability" });
        _menuSteps = Get<GetMenuSteps>();
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

    [Fact]
    [HappyPath]
    public async Task Menu_endpoint_should_return_all_menu_items_with_availability()
    {
        // When the menu is requested
        await _menuSteps.Retrieve();

        // Then the menu response should contain all menu items
        Track.That(() => _menuSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _menuSteps.ParseResponse();
        Track.That(() => _menuSteps.Response!.Should().Contain(m => m.Name == MenuDefaults.ClassicPancakes));
        Track.That(() => _menuSteps.Response!.Should().Contain(m => m.Name == MenuDefaults.BelgianWaffles));
        Track.That(() => _menuSteps.Response!.Should().Contain(m => m.Name == MenuDefaults.GoatMilkPancakes));

        // And the menu items should be in alphabetical order
        Track.That(() => _menuSteps.Response!.Should().BeInAscendingOrder(m => m.Name));

        // And the supplier service should have received an availability request
        if (!Settings.RunAgainstExternalServiceUnderTest)
            _downstreamSteps.AssertSupplierServiceReceivedAvailabilityRequest();
    }
}
