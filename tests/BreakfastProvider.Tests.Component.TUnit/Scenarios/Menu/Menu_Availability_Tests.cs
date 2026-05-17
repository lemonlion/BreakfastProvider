using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Menu;
using BreakfastProvider.Tests.Component.Shared.Constants;
using Kronikol.TUnit;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.Menu;

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

    [Test]
    [HappyPath]
    public async Task Menu_endpoint_should_return_all_menu_items_with_availability()
    {
        // When the menu is requested
        await _menuSteps.Retrieve();

        // Then the menu response should contain all menu items
        await _menuSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _menuSteps.ParseResponse();
        await _menuSteps.Response!.Should().Contain(m => m.Name == MenuDefaults.ClassicPancakes);
        await _menuSteps.Response!.Should().Contain(m => m.Name == MenuDefaults.BelgianWaffles);
        await _menuSteps.Response!.Should().Contain(m => m.Name == MenuDefaults.GoatMilkPancakes);

        // And the menu items should be in alphabetical order
        await _menuSteps.Response!.Select(m => m.Name).Should().BeInOrder();

        // And the supplier service should have received an availability request
        if (!Settings.RunAgainstExternalServiceUnderTest)
            _downstreamSteps.AssertSupplierServiceReceivedAvailabilityRequest();
    }
}
