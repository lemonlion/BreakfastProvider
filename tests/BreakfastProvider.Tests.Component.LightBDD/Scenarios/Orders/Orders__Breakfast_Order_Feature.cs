using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Orders;

[FeatureDescription($"/{Endpoints.Orders} - Creating and managing breakfast orders with event publishing")]
public partial class Orders__Breakfast_Order_Feature
{
    [HappyPath]
    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsEventAndKafkaInfrastructure)]
    public async Task A_Valid_Order_Should_Be_Created_And_An_Event_Published()
    {
        await Runner.RunScenarioAsync(
            given => A_pancake_batch_has_been_created(),
            and => A_valid_order_request_for_the_created_batch(),
            when => The_breakfast_order_is_placed(),
            then => The_order_response_should_contain_a_complete_order(),
            and => An_order_created_event_should_have_been_published(),
            and => The_kitchen_service_should_have_received_a_preparation_request());
    }

    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsEventAndKafkaInfrastructure)]
    public async Task Creating_An_Order_Should_Produce_An_Audit_Log_Entry_And_Events()
    {
        await Runner.RunScenarioAsync(
            given => A_pancake_batch_has_been_created(),
            and => A_valid_order_request_for_the_created_batch(),
            when => The_breakfast_order_is_placed(),
            then => The_order_response_should_contain_a_complete_order(),
            and => An_order_created_event_should_have_been_published(),
            and => A_recipe_log_should_have_been_published_to_kafka());
    }

    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsEventAndKafkaInfrastructure)]
    public async Task Creating_An_Order_Should_Write_An_Outbox_Message_That_Gets_Processed()
    {
        await Runner.RunScenarioAsync(
            given => A_pancake_batch_has_been_created(),
            and => A_valid_order_request_for_the_created_batch(),
            when => The_breakfast_order_is_placed(),
            then => The_order_response_should_contain_a_complete_order(),
            and => An_outbox_message_should_have_been_written_for_the_order_created_event(),
            and => The_outbox_message_should_have_been_processed());
    }
}