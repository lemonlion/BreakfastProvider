using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Infrastructure;

[FeatureDescription($"/{Endpoints.Health} - Health check endpoint with dependency status for monitoring")]
public partial class Infrastructure__Health_Check_Feature
{
    [HappyPath]
    [Scenario]
    public async Task The_Health_Check_Endpoint_Should_Return_A_Healthy_Status_With_Dependency_Details()
    {
        await Runner.RunScenarioAsync(
            when => The_health_check_endpoint_is_called(),
            then => The_health_check_response_should_indicate_healthy_with_all_dependencies());
    }
}
