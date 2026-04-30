using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.AuditLogs;

[FeatureDescription($"/{Endpoints.AuditLogs} - Retrieving audit log entries for order operations")]
public partial class AuditLogs__Retrieval_Feature
{
    [HappyPath]
    [Scenario]
    public async Task Creating_An_Order_Should_Produce_A_Retrievable_Audit_Log_Entry()
    {
        await Runner.RunScenarioAsync(
            given => A_pancake_batch_has_been_created(),
            and => An_order_has_been_created_for_the_batch(),
            when => The_audit_logs_are_retrieved(),
            then => The_audit_log_response_should_contain_the_order_creation_entry(),
            and => The_cow_service_should_have_received_a_milk_request(),
            and => The_kitchen_service_should_have_received_a_preparation_request());
    }
}
