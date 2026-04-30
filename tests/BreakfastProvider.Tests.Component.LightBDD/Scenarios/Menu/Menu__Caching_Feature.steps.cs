using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Menu;
using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Menu;

#pragma warning disable CS1998
public partial class Menu__Caching_Feature : BaseFixture
{
    private readonly GetMenuSteps _menuSteps;
    private readonly GetMenuSteps _secondMenuSteps;

    public Menu__Caching_Feature() : base(delayAppCreation: true)
    {
        CreateAppAndClient(new Dictionary<string, string?> { ["TestIsolation"] = "MenuCaching" });
        _menuSteps = Get<GetMenuSteps>();
        _secondMenuSteps = Get<GetMenuSteps>();
    }

    #region Given

    private async Task<CompositeStep> The_menu_has_been_requested_and_cached()
    {
        return Sub.Steps(
            _ => The_menu_cache_is_cleared(),
            _ => The_first_menu_request_is_sent(),
            _ => The_first_menu_response_should_be_successful());
    }

    private async Task The_menu_cache_is_cleared()
        => await Client.DeleteAsync(Endpoints.MenuCache);

    private async Task The_first_menu_request_is_sent()
        => await _menuSteps.Retrieve();

    private async Task The_first_menu_response_should_be_successful()
        => _menuSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_supplier_service_is_then_made_unavailable()
        => _secondMenuSteps.AddHeader(FakeScenarioHeaders.SupplierService, FakeScenarios.ServiceUnavailable);

    #endregion

    #region When

    private async Task The_menu_is_requested_again()
        => await _secondMenuSteps.Retrieve();

    #endregion

    #region Then

    private async Task<CompositeStep> The_menu_response_should_still_return_available_items()
    {
        return Sub.Steps(
            _ => The_cached_menu_response_http_status_should_be_ok(),
            _ => The_cached_menu_list_should_be_valid_json(),
            _ => The_cached_menu_should_contain_available_items());
    }

    private async Task The_cached_menu_response_http_status_should_be_ok()
        => _secondMenuSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_cached_menu_list_should_be_valid_json()
        => await _secondMenuSteps.ParseResponse();

    private async Task The_cached_menu_should_contain_available_items()
        => _secondMenuSteps.Response!.Should().Contain(m => m.IsAvailable);

    #endregion
}
