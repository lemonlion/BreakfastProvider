using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Menu;

[FeatureDescription($"/{Endpoints.Menu} - Retrieving the breakfast menu with ingredient availability")]
public partial class Menu__Availability_Feature
{
    [HappyPath]
    [Scenario]
    public async Task The_Menu_Endpoint_Should_Return_All_Menu_Items_With_Availability()
    {
        await Runner.RunScenarioAsync(
            when => The_menu_is_requested(),
            then => The_menu_response_should_contain_all_menu_items(),
            and => The_menu_items_should_be_in_alphabetical_order(),
            and => The_supplier_service_should_have_received_an_availability_request());
    }
}
