using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Menu;

[FeatureDescription($"/{Endpoints.Menu} - Menu response caching behaviour")]
public partial class Menu__Caching_Feature
{
    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsToControlFakeResponses)]
    public async Task The_Menu_Should_Return_Cached_Results_On_Subsequent_Requests()
    {
        await Runner.RunScenarioAsync(
            given => The_menu_has_been_requested_and_cached(),
            and => The_supplier_service_is_then_made_unavailable(),
            when => The_menu_is_requested_again(),
            then => The_menu_response_should_still_return_available_items());
    }
}
