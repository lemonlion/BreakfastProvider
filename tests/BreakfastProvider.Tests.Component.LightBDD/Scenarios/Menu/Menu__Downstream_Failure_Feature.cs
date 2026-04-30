using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Menu;

[FeatureDescription($"/{Endpoints.Menu} - Handling downstream Supplier Service failures")]
public partial class Menu__Downstream_Failure_Feature
{
    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsToControlFakeResponses)]
    public async Task Requesting_The_Menu_When_The_Supplier_Service_Is_Unavailable_Should_Mark_Items_As_Unavailable()
    {
        await Runner.RunScenarioAsync(
            given => The_supplier_service_will_return_service_unavailable(),
            when => The_menu_is_requested(),
            then => The_menu_response_should_mark_all_items_as_unavailable());
    }
}
