using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using BreakfastProvider.Tests.Component.LightBDD.Infrastructure;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.CustomerFeedback;

[FeatureDescription("PubSub → BreakfastProvider → MongoDB + gRPC + HTTP: Customer feedback event consumption and downstream processing")]
public partial class Customer_Feedback__Alert_Feature
{
    [HappyPath]
    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsInMemoryEventConsumer)]
    public async Task Consuming_Customer_Feedback_Event_Should_Trigger_Downstream_Processing()
    {
        await Runner.RunScenarioAsync(
            given => A_customer_feedback_received_event(),
            when => The_event_is_published_to_pubsub(),
            then => The_feedback_id_should_be_generated(),
            and => The_supplier_service_should_have_received_the_feedback());
    }
}
