using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Orders;

[FeatureDescription($"/{Endpoints.Orders} - Outbox message transitions to failed after exhausting retries")]
public partial class Orders__Outbox_Retry_Exhaustion_Feature
{
    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsEventAndKafkaInfrastructure)]
    public async Task An_Outbox_Message_Should_Transition_To_Failed_After_Exhausting_Retries()
    {
        await Runner.RunScenarioAsync(
            given => A_pancake_batch_has_been_created(),
            and => A_valid_order_request_for_the_created_batch(),
            when => The_breakfast_order_is_placed(),
            then => The_order_should_be_created_successfully(),
            and => The_outbox_message_should_transition_to_failed());
    }
}
