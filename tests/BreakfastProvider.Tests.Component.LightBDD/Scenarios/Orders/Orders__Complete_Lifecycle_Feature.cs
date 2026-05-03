using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Orders;

[FeatureDescription($"/{Endpoints.Orders} - Complete order lifecycle from creation through to completion")]
public partial class Orders__Complete_Lifecycle_Feature
{
    [HappyPath]
    [Scenario]
    public async Task An_Order_Should_Progress_Through_All_Status_Transitions_To_Completion()
    {
        await Runner.RunScenarioAsync(
            given => A_pancake_batch_has_been_created(),
            and => A_breakfast_order_has_been_placed_for_the_batch(),
            when => The_order_is_progressed_through_the_complete_lifecycle(),
            then => The_completed_order_should_be_retrievable_with_all_details(),
            and => The_order_timestamps_should_be_recent(),
            and => The_order_id_should_be_a_valid_guid_format(),
            and => An_audit_log_entry_should_exist_for_the_order(),
            and => The_cow_service_should_have_received_a_milk_request(),
            and => The_kitchen_service_should_have_received_a_preparation_request());
    }
}
