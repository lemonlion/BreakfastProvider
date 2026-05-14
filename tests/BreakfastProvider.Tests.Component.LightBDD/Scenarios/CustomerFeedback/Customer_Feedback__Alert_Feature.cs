using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.CustomerFeedback;

[FeatureDescription($"/{Endpoints.CustomerFeedback} - Customer feedback alert processing (PubSub → MongoDB → gRPC → HTTP)")]
public partial class Customer_Feedback__Alert_Feature
{
    [HappyPath]
    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsEventAndKafkaInfrastructure)]
    public async Task Submitting_Customer_Feedback_Should_Trigger_Event_Consumption_And_Downstream_Calls()
    {
        await Runner.RunScenarioAsync(
            given => A_valid_customer_feedback_request(),
            when => The_feedback_is_submitted(),
            then => The_response_should_be_accepted(),
            and => The_supplier_service_should_have_received_the_feedback());
    }
}
