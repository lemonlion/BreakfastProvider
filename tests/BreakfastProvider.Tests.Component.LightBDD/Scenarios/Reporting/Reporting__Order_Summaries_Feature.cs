using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Reporting;

[FeatureDescription($"/{Endpoints.GraphQL} - Querying order summary reports via GraphQL")]
public partial class Reporting__Order_Summaries_Feature
{
    [HappyPath]
    [Scenario]
    public async Task Order_Summaries_Should_Contain_Ingested_Order_Data()
    {
        await Runner.RunScenarioAsync(
            given => A_pancake_batch_has_been_created(),
            and => A_breakfast_order_has_been_placed_for_the_batch(),
            when => The_order_summaries_are_queried_via_graphql(),
            then => The_graphql_response_should_contain_the_ingested_order_summary());
    }

    [Scenario]
    public async Task Order_Summaries_Should_Return_An_Empty_List_When_No_Orders_Exist()
    {
        await Runner.RunScenarioAsync(
            when => The_order_summaries_are_queried_via_graphql(),
            then => The_graphql_response_should_be_successful(),
            and => The_order_summaries_list_should_be_empty_or_not_contain_the_test_order());
    }
}
