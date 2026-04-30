using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.AuditLogs;

[FeatureDescription($"/{Endpoints.AuditLogs} - Filtering audit logs by entity type and entity ID")]
public partial class AuditLogs__Filtering_Feature
{
    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsEventAndKafkaInfrastructure)]
    public async Task Audit_Logs_Should_Be_Filterable_By_Entity_Type()
    {
        await Runner.RunScenarioAsync(
            given => An_order_has_been_created_to_generate_an_audit_log(),
            when => Audit_logs_are_requested_filtered_by_entity_type(),
            then => The_audit_log_response_should_only_contain_order_entries());
    }

    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsEventAndKafkaInfrastructure)]
    public async Task Audit_Logs_Should_Be_Filterable_By_Entity_Id()
    {
        await Runner.RunScenarioAsync(
            given => An_order_has_been_created_to_generate_an_audit_log(),
            when => Audit_logs_are_requested_filtered_by_entity_id(),
            then => The_audit_log_response_should_contain_the_specific_order_entry());
    }

    [Scenario]
    public async Task Filtering_Audit_Logs_By_A_Non_Existent_Entity_Type_Should_Return_An_Empty_Collection()
    {
        await Runner.RunScenarioAsync(
            when => Audit_logs_are_requested_filtered_by_a_non_existent_entity_type(),
            then => The_audit_log_response_should_be_an_empty_collection());
    }

    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsEventAndKafkaInfrastructure)]
    public async Task Audit_Logs_Should_Be_Returned_In_Descending_Timestamp_Order()
    {
        await Runner.RunScenarioAsync(
            given => An_order_has_been_created_to_generate_an_audit_log(),
            when => Audit_logs_are_requested_filtered_by_entity_type(),
            then => The_audit_logs_should_be_ordered_by_timestamp_descending());
    }
}
