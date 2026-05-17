using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Menu;
using BreakfastProvider.Tests.Component.Shared.Constants;

using TestStack.BDDfy;
using Kronikol.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Menu;

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
    public void Menu_should_return_cached_results_on_subsequent_requests()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        this.Given(x => x.The_menu_has_been_requested_and_cached())
            .And(x => x.The_supplier_service_is_then_made_unavailable())
            .When(x => x.The_menu_is_requested_again())
            .Then(x => x.The_menu_response_should_still_return_available_items())
            .BDDfy();
    }

    #region Steps

    private async Task The_menu_has_been_requested_and_cached()
    {
        await Client.DeleteAsync(Endpoints.MenuCache);
        await _menuSteps.Retrieve();
        _menuSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private void The_supplier_service_is_then_made_unavailable()
    {
        _secondMenuSteps.AddHeader(FakeScenarioHeaders.SupplierService, FakeScenarios.ServiceUnavailable);
    }

    private async Task The_menu_is_requested_again()
    {
        await _secondMenuSteps.Retrieve();
    }

    private async Task The_menu_response_should_still_return_available_items()
    {
        _secondMenuSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _secondMenuSteps.ParseResponse();
        _secondMenuSteps.Response!.Should().Contain(m => m.IsAvailable);
    }

    #endregion
}
