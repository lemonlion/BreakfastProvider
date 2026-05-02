using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Reporting;

[FeatureDescription($"/{Endpoints.GraphQL} - Querying equipment alerts populated by Event Hub consumption")]
public partial class Reporting__Equipment_Alerts_Feature
{
    [HappyPath]
    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), IgnoreReasons.NeedsInProcessEventConsumers)]
    public async Task Equipment_Alerts_Should_Contain_Data_Ingested_Via_Event_Hub_Consumer()
    {
        await Runner.RunScenarioAsync(
            given => A_pancake_batch_has_been_created(),
            when => The_equipment_alerts_are_queried_via_graphql(),
            then => The_graphql_response_should_contain_the_equipment_alert_record());
    }
}
