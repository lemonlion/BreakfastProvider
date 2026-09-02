using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using BreakfastProvider.Tests.Component.LightBDD.Infrastructure;
using Kronikol.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.ServiceTimes;

[FeatureDescription("Order served events - Service time analysis (Kafka → ClickHouse → gRPC → Kitchen)")]
public partial class Service_Times__Analysis_Feature
{
    [HappyPath]
    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsInMemoryEventConsumer)]
    public async Task Consuming_An_Order_Served_Event_Should_Trigger_Downstream_Processing()
    {
        await Runner.RunScenarioAsync(
            given => An_order_served_event(),
            when => The_event_is_published_to_kafka(),
            then => The_order_id_should_be_generated(),
            and => The_kitchen_service_should_have_received_the_status_request());
    }
}
