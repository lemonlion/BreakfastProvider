using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Infrastructure;

[FeatureDescription($"Cross-cutting - {CustomHeaders.CorrelationId} header propagation across API responses")]
public partial class Infrastructure__Correlation_Id_Feature
{
    [HappyPath]
    [Scenario]
    public async Task A_Request_With_A_Correlation_Id_Should_Return_The_Same_Id_In_The_Response()
    {
        await Runner.RunScenarioAsync(
            given => A_request_with_a_known_correlation_id(),
            when => The_request_is_sent_to_the_menu_endpoint(),
            then => The_response_should_contain_the_same_correlation_id());
    }

    [Scenario]
    public async Task A_Request_Without_A_Correlation_Id_Should_Have_One_Generated_In_The_Response()
    {
        await Runner.RunScenarioAsync(
            when => A_request_without_a_correlation_id_is_sent_to_the_menu_endpoint(),
            then => The_response_should_contain_a_generated_correlation_id());
    }
}
