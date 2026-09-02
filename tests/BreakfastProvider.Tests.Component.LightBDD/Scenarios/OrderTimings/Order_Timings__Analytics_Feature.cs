using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using Kronikol.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.OrderTimings;

[FeatureDescription($"/{Endpoints.OrderTimings} - Recording and summarising kitchen order timings (ClickHouse)")]
public partial class Order_Timings__Analytics_Feature
{
    [HappyPath]
    [Scenario]
    public async Task Recording_An_Order_Timing_Should_Return_The_Created_Record()
    {
        await Runner.RunScenarioAsync(
            given => A_valid_order_timing_request(),
            when => The_order_timing_is_recorded(),
            then => The_timing_response_should_contain_the_created_record());
    }

    [Scenario]
    public async Task Listing_Timings_By_Station_Should_Return_Matching_Records()
    {
        await Runner.RunScenarioAsync(
            given => An_order_timing_record_has_been_created(),
            when => The_timings_are_listed_by_station(),
            then => The_timing_list_response_should_contain_the_record());
    }

    [Scenario]
    public async Task Getting_The_Timing_Summary_Should_Return_Aggregated_Data()
    {
        await Runner.RunScenarioAsync(
            given => An_order_timing_record_has_been_created(),
            when => The_timing_summary_is_requested(),
            then => The_summary_should_contain_aggregated_data_for_the_station());
    }

    [Scenario]
    public async Task Recording_A_Timing_With_Missing_Station_Should_Return_Bad_Request()
    {
        await Runner.RunScenarioAsync(
            given => An_order_timing_request_with_a_missing_station(),
            when => The_order_timing_is_recorded(),
            then => The_timing_post_response_should_indicate_bad_request());
    }

    [Scenario]
    public async Task Recording_A_Timing_With_Zero_Prep_Seconds_Should_Return_Bad_Request()
    {
        await Runner.RunScenarioAsync(
            given => An_order_timing_request_with_zero_prep_seconds(),
            when => The_order_timing_is_recorded(),
            then => The_timing_post_response_should_indicate_bad_request());
    }
}
