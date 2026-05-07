using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Menu;
using BreakfastProvider.Tests.Component.Shared.Constants;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Menu;

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
    public void Menu_endpoint_should_return_all_menu_items_with_availability()
    {
        this.When(x => x.The_menu_is_requested())
            .Then(x => x.The_menu_response_should_contain_all_menu_items())
            .And(x => x.The_menu_items_should_be_in_alphabetical_order())
            .And(x => x.The_supplier_service_should_have_received_an_availability_request())
            .BDDfy();
    }

    #region Steps

    private async Task The_menu_is_requested()
    {
        await _menuSteps.Retrieve();
    }

    private async Task The_menu_response_should_contain_all_menu_items()
    {
        _menuSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _menuSteps.ParseResponse();
        _menuSteps.Response!.Should().Contain(m => m.Name == MenuDefaults.ClassicPancakes);
        _menuSteps.Response!.Should().Contain(m => m.Name == MenuDefaults.BelgianWaffles);
        _menuSteps.Response!.Should().Contain(m => m.Name == MenuDefaults.GoatMilkPancakes);
    }

    private void The_menu_items_should_be_in_alphabetical_order()
    {
        _menuSteps.Response!.Should().BeInAscendingOrder(m => m.Name);
    }

    private void The_supplier_service_should_have_received_an_availability_request()
    {
        if (!Settings.RunAgainstExternalServiceUnderTest)
            _downstreamSteps.AssertSupplierServiceReceivedAvailabilityRequest();
    }

    #endregion
}
