using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Menu;
using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Menu;

#pragma warning disable CS1998
public partial class Menu__Downstream_Failure_Feature : BaseFixture
{
    private readonly GetMenuSteps _menuSteps;

    public Menu__Downstream_Failure_Feature() : base(delayAppCreation: true)
    {
        CreateAppAndClient(new Dictionary<string, string?> { ["TestIsolation"] = "MenuDownstreamFailure" });
        _menuSteps = Get<GetMenuSteps>();
    }

    #region Given

    private async Task The_supplier_service_will_return_service_unavailable()
    {
        // Clear cache to ensure this test gets a fresh response
        await Client.DeleteAsync(Endpoints.MenuCache);
        _menuSteps.AddHeader(FakeScenarioHeaders.SupplierService, FakeScenarios.ServiceUnavailable);
    }

    #endregion

    #region When

    private async Task The_menu_is_requested()
        => await _menuSteps.Retrieve();

    #endregion

    #region Then

    private async Task<CompositeStep> The_menu_response_should_mark_all_items_as_unavailable()
    {
        return Sub.Steps(
            _ => The_menu_response_http_status_should_be_ok(),
            _ => The_menu_list_should_be_valid_json(),
            _ => All_menu_items_should_be_marked_as_unavailable());
    }

    private async Task The_menu_response_http_status_should_be_ok()
        => _menuSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_menu_list_should_be_valid_json()
        => await _menuSteps.ParseResponse();

    private async Task All_menu_items_should_be_marked_as_unavailable()
        => _menuSteps.Response!.Should().OnlyContain(m => m.IsAvailable == false);

    #endregion
}
