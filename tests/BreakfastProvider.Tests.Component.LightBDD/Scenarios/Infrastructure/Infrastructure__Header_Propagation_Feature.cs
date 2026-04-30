using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Infrastructure;

[FeatureDescription($"/{Endpoints.Milk}; /{Endpoints.Menu} - X-Correlation-Id header propagation to downstream services")]
public partial class Infrastructure__Header_Propagation_Feature
{
    [HappyPath]
    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsToControlFakeResponses)]
    public async Task A_Request_With_A_Correlation_Id_Should_Forward_It_To_The_Cow_Service()
    {
        await Runner.RunScenarioAsync(
            given => A_request_with_a_known_correlation_id(),
            when => Milk_is_requested_from_the_milk_endpoint(),
            then => The_cow_service_should_have_received_the_correlation_id());
    }

    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsToControlFakeResponses)]
    public async Task A_Request_With_A_Correlation_Id_Should_Forward_It_To_The_Supplier_Service()
    {
        await Runner.RunScenarioAsync(
            given => A_request_with_a_known_correlation_id(),
            and => The_menu_cache_is_cleared(),
            when => The_menu_is_requested(),
            then => The_supplier_service_should_have_received_the_correlation_id());
    }
}
