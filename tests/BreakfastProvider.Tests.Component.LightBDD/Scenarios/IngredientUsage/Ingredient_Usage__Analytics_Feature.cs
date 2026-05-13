using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.IngredientUsage;

[FeatureDescription($"/{Endpoints.IngredientUsage} - Recording and summarising ingredient usage (BigQuery)")]
public partial class Ingredient_Usage__Analytics_Feature
{
    [HappyPath]
    [Scenario]
    public async Task Recording_Ingredient_Usage_Should_Return_The_Created_Record()
    {
        await Runner.RunScenarioAsync(
            given => A_valid_ingredient_usage_request(),
            when => The_usage_is_recorded(),
            then => The_response_should_contain_the_created_record());
    }

    [Scenario]
    public async Task Listing_Usage_By_Ingredient_Should_Return_Matching_Records()
    {
        await Runner.RunScenarioAsync(
            given => An_ingredient_usage_record_exists(),
            when => The_usage_is_listed_by_ingredient(),
            then => The_list_response_should_contain_the_record());
    }

    [Scenario]
    public async Task Getting_Usage_Summary_Should_Return_Aggregated_Data()
    {
        await Runner.RunScenarioAsync(
            given => An_ingredient_usage_record_exists(),
            when => The_summary_is_requested(),
            then => The_summary_should_contain_aggregated_data());
    }

    [Scenario]
    public async Task Recording_Usage_With_Missing_Ingredient_Name_Should_Return_Bad_Request()
    {
        await Runner.RunScenarioAsync(
            given => A_usage_request_with_missing_ingredient_name(),
            when => The_usage_is_recorded(),
            then => The_usage_response_should_indicate_bad_request());
    }

    [Scenario]
    public async Task Recording_Usage_With_Zero_Quantity_Should_Return_Bad_Request()
    {
        await Runner.RunScenarioAsync(
            given => A_usage_request_with_zero_quantity(),
            when => The_usage_is_recorded(),
            then => The_usage_response_should_indicate_bad_request());
    }
}
