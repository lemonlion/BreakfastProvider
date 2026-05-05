using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Menu;
using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Menu;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class Menu__Availability_Feature : BaseFixture
{
    private readonly GetMenuSteps _menuSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;

    public Menu__Availability_Feature() : base(delayAppCreation: true)
    {
        CreateAppAndClient(new Dictionary<string, string?> { ["TestIsolation"] = "MenuAvailability" });
        _menuSteps = Get<GetMenuSteps>();
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

    #region Given
    #endregion

    #region When

    private async Task The_menu_is_requested()
        => await _menuSteps.Retrieve();

    #endregion

    #region Then

    private async Task<CompositeStep> The_menu_response_should_contain_all_menu_items()
    {
        return Sub.Steps(
            _ => The_menu_response_http_status_should_be_ok(),
            _ => The_menu_list_should_be_valid_json(),
            _ => The_menu_should_contain_classic_pancakes(),
            _ => The_menu_should_contain_belgian_waffles(),
            _ => The_menu_should_contain_goat_milk_pancakes());
    }

    private async Task The_menu_response_http_status_should_be_ok()
        => _menuSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_menu_list_should_be_valid_json()
        => await _menuSteps.ParseResponse();

    private async Task The_menu_should_contain_classic_pancakes()
        => _menuSteps.Response!.Should().Contain(m => m.Name == MenuDefaults.ClassicPancakes);

    private async Task The_menu_should_contain_belgian_waffles()
        => _menuSteps.Response!.Should().Contain(m => m.Name == MenuDefaults.BelgianWaffles);

    private async Task The_menu_should_contain_goat_milk_pancakes()
        => _menuSteps.Response!.Should().Contain(m => m.Name == MenuDefaults.GoatMilkPancakes);

    private async Task The_menu_items_should_be_in_alphabetical_order()
    {
        await _menuSteps.ParseResponse();
        _menuSteps.Response!.Should().BeInAscendingOrder(m => m.Name);
    }

    [SkipStepIf(nameof(Settings.RunAgainstExternalServiceUnderTest), DownstreamFakeRequestStoreIsUnavailableInPostDeploymentEnvironments)]
    private async Task The_supplier_service_should_have_received_an_availability_request()
        => _downstreamSteps.AssertSupplierServiceReceivedAvailabilityRequest();

    #endregion
}
