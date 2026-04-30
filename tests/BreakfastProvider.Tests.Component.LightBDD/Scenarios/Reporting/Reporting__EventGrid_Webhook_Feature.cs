using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Reporting;

[FeatureDescription($"/{Endpoints.EventGridWebhook} - Receiving and processing EventGrid webhook events")]
public partial class Reporting__EventGrid_Webhook_Feature
{
    [HappyPath]
    [Scenario]
    public async Task Ingredient_Shipments_Should_Be_Recorded_When_Delivered_Via_EventGrid_Webhook()
    {
        await Runner.RunScenarioAsync(
            given => An_ingredient_delivery_event_has_been_received_via_eventgrid_webhook(),
            when => The_ingredient_shipments_are_queried_via_graphql(),
            then => The_graphql_response_should_contain_the_ingredient_shipment());
    }
}
