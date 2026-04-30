using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Reporting;

[FeatureDescription($"/{Endpoints.GraphQL} - Querying batch completion records populated by Pub/Sub consumption")]
public partial class Reporting__Batch_Completions_Feature
{
    [HappyPath]
    [Scenario]
    public async Task Batch_Completions_Should_Contain_Data_Ingested_Via_PubSub_Consumer()
    {
        await Runner.RunScenarioAsync(
            given => A_pancake_batch_has_been_created(),
            when => The_batch_completions_are_queried_via_graphql(),
            then => The_graphql_response_should_contain_the_batch_completion_record());
    }
}
