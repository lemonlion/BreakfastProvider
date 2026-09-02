using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using Kronikol.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.EquipmentReadings;

[FeatureDescription($"/{Endpoints.EquipmentReadings} - Recording and monitoring kitchen equipment readings (ClickHouse)")]
public partial class Equipment_Readings__Monitoring_Feature
{
    [HappyPath]
    [Scenario]
    public async Task Recording_An_Equipment_Reading_Should_Return_The_Created_Record()
    {
        await Runner.RunScenarioAsync(
            given => A_valid_equipment_reading_request(),
            when => The_equipment_reading_is_recorded(),
            then => The_reading_response_should_contain_the_created_record());
    }

    [Scenario]
    public async Task Listing_Readings_By_Equipment_Should_Return_Matching_Records()
    {
        await Runner.RunScenarioAsync(
            given => An_equipment_reading_record_has_been_created(),
            when => The_readings_are_listed_by_equipment(),
            then => The_reading_list_response_should_contain_the_record());
    }

    [Scenario]
    public async Task Deleting_A_Reading_Should_Remove_It_From_The_List()
    {
        await Runner.RunScenarioAsync(
            given => An_equipment_reading_record_has_been_created(),
            when => The_reading_is_deleted(),
            then => The_delete_response_should_indicate_no_content(),
            and => The_reading_should_no_longer_be_listed());
    }

    [Scenario]
    public async Task Recording_A_Reading_With_Missing_Metric_Should_Return_Bad_Request()
    {
        await Runner.RunScenarioAsync(
            given => An_equipment_reading_request_with_a_missing_metric(),
            when => The_equipment_reading_is_recorded(),
            then => The_reading_post_response_should_indicate_bad_request());
    }

    [Scenario]
    public async Task Recording_A_Reading_With_Zero_Value_Should_Return_Bad_Request()
    {
        await Runner.RunScenarioAsync(
            given => An_equipment_reading_request_with_zero_value(),
            when => The_equipment_reading_is_recorded(),
            then => The_reading_post_response_should_indicate_bad_request());
    }
}
