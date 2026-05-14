using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.RecipeCosts;

[FeatureDescription($"/{Endpoints.RecipeCosts} - Recipe cost analysis processing (Kafka → BigQuery → gRPC → HTTP)")]
public partial class Recipe_Costs__Analysis_Feature
{
    [HappyPath]
    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsEventAndKafkaInfrastructure)]
    public async Task Submitting_Recipe_Cost_Should_Trigger_Event_Consumption_And_Downstream_Calls()
    {
        await Runner.RunScenarioAsync(
            given => A_valid_recipe_cost_request(),
            when => The_cost_calculation_is_submitted(),
            then => The_response_should_be_accepted(),
            and => The_kitchen_service_should_have_received_the_preparation_request());
    }
}
