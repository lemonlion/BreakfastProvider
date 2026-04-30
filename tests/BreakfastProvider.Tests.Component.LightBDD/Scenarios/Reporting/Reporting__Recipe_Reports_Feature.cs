using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Reporting;

[FeatureDescription($"/{Endpoints.GraphQL} - Querying recipe reports and aggregations via GraphQL")]
public partial class Reporting__Recipe_Reports_Feature
{
    [HappyPath]
    [Scenario]
    public async Task Recipe_Reports_Should_Contain_Ingested_Recipe_Data()
    {
        await Runner.RunScenarioAsync(
            given => A_pancake_batch_has_been_created(),
            when => The_recipe_reports_are_queried_via_graphql(),
            then => The_graphql_response_should_contain_the_ingested_recipe_reports());
    }

    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsDirectDatabaseAccess)]
    public async Task Ingredient_Usage_Should_Aggregate_Across_Multiple_Recipes()
    {
        await Runner.RunScenarioAsync(
            given => Multiple_recipe_logs_have_been_ingested_with_overlapping_ingredients(),
            when => The_ingredient_usage_is_queried_via_graphql(),
            then => The_ingredient_usage_should_reflect_aggregated_counts());
    }

    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsDirectDatabaseAccess)]
    public async Task Popular_Recipes_Should_Return_Recipe_Types_Ordered_By_Frequency()
    {
        await Runner.RunScenarioAsync(
            given => Multiple_recipe_logs_of_different_types_have_been_ingested(),
            when => The_popular_recipes_are_queried_via_graphql(),
            then => The_popular_recipes_should_be_ordered_by_count_descending());
    }
}
