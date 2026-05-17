using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using BreakfastProvider.Tests.Component.LightBDD.Infrastructure;
using Kronikol.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.RecipeCosts;

[FeatureDescription("Kafka → BreakfastProvider → BigQuery + gRPC + HTTP: Recipe cost event consumption and downstream processing")]
public partial class Recipe_Costs__Analysis_Feature
{
    [HappyPath]
    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsInMemoryEventConsumer)]
    public async Task Consuming_Recipe_Cost_Event_Should_Trigger_Downstream_Processing()
    {
        await Runner.RunScenarioAsync(
            given => A_recipe_cost_calculated_event(),
            when => The_event_is_published_to_kafka(),
            then => The_calculation_id_should_be_generated(),
            and => The_kitchen_service_should_have_received_the_preparation_request());
    }
}
