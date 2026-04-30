using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Infrastructure;

[FeatureDescription($"/{Endpoints.Health} - Health check response includes detailed entry descriptions and data")]
public partial class Infrastructure__Health_Check_Detail_Feature
{
    [Scenario]
    public async Task The_Health_Check_Response_Should_Include_Description_And_Data_For_Each_Entry()
    {
        await Runner.RunScenarioAsync(
            when => The_health_check_endpoint_is_called(),
            then => The_health_check_response_should_contain_detailed_entries());
    }
}
