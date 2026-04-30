using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Infrastructure;

[FeatureDescription($"/{Endpoints.Heartbeat} - Heartbeat endpoint confirming the service is running")]
public partial class Infrastructure__Heartbeat_Feature
{
    [HappyPath]
    [Scenario]
    public async Task The_Heartbeat_Endpoint_Should_Return_A_Running_Message()
    {
        await Runner.RunScenarioAsync(
            when => The_heartbeat_endpoint_is_called(),
            then => The_heartbeat_response_should_indicate_the_service_is_running());
    }
}
