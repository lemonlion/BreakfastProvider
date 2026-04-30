using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Infrastructure;

[FeatureDescription($"/{Endpoints.Health} - Health check reports degraded when a downstream service returns a non-success HTTP status")]
public partial class Infrastructure__Downstream_Error_Health_Check_Feature
{
    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsToControlFakeResponses)]
    public async Task Health_Check_Should_Report_Degraded_When_A_Downstream_Service_Returns_A_Non_Success_Status()
    {
        await Runner.RunScenarioAsync(
            given => The_kitchen_service_health_check_is_configured_to_use_a_failing_endpoint(),
            when => The_health_check_endpoint_is_called(),
            then => The_health_check_response_should_indicate_a_degraded_status(),
            and => The_kitchen_service_dependency_should_report_degraded_with_a_status_code_description());
    }
}
