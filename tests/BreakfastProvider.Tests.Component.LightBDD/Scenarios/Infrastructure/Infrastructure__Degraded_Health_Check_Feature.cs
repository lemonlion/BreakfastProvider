using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Infrastructure;

[FeatureDescription($"/{Endpoints.Health} - Health check endpoint reporting degraded status when downstream services are unavailable")]
public partial class Infrastructure__Degraded_Health_Check_Feature
{
    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsToControlFakeResponses)]
    public async Task Health_Check_Should_Report_Degraded_When_The_Cow_Service_Is_Unavailable()
    {
        await Runner.RunScenarioAsync(
            given => The_cow_service_is_configured_to_be_unreachable(),
            when => The_health_check_endpoint_is_called(),
            then => The_health_check_response_should_indicate_a_degraded_status(),
            and => The_cow_service_dependency_should_report_degraded());
    }

    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsToControlFakeResponses)]
    public async Task Health_Check_Should_Report_Degraded_When_Multiple_Downstream_Services_Are_Unavailable()
    {
        await Runner.RunScenarioAsync(
            given => The_cow_service_is_configured_to_be_unreachable(),
            and => The_supplier_service_is_configured_to_be_unreachable(),
            when => The_health_check_endpoint_is_called(),
            then => The_health_check_response_should_indicate_a_degraded_status(),
            and => The_cow_service_dependency_should_report_degraded(),
            and => The_supplier_service_dependency_should_report_degraded());
    }
}
