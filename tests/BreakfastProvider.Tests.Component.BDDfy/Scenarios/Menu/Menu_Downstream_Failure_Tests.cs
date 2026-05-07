using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Menu;
using BreakfastProvider.Tests.Component.Shared.Constants;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Menu;

public class Menu_Downstream_Failure_Tests : BaseFixture
{
    private readonly GetMenuSteps _menuSteps;

    public Menu_Downstream_Failure_Tests() : base(delayAppCreation: true)
    {
        CreateAppAndClient(new Dictionary<string, string?> { ["TestIsolation"] = "MenuDownstreamFailure" });
        _menuSteps = Get<GetMenuSteps>();
    }

    [Fact]
    public void Requesting_menu_when_supplier_service_unavailable_should_mark_items_as_unavailable()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        this.Given(x => x.The_supplier_service_will_return_service_unavailable())
            .When(x => x.The_menu_is_requested())
            .Then(x => x.The_menu_response_should_mark_all_items_as_unavailable())
            .BDDfy();
    }

    #region Steps

    private async Task The_supplier_service_will_return_service_unavailable()
    {
        await Client.DeleteAsync(Endpoints.MenuCache);
        _menuSteps.AddHeader(FakeScenarioHeaders.SupplierService, FakeScenarios.ServiceUnavailable);
    }

    private async Task The_menu_is_requested()
    {
        await _menuSteps.Retrieve();
    }

    private async Task The_menu_response_should_mark_all_items_as_unavailable()
    {
        _menuSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _menuSteps.ParseResponse();
        _menuSteps.Response!.Should().OnlyContain(m => m.IsAvailable == false);
    }

    #endregion
}
