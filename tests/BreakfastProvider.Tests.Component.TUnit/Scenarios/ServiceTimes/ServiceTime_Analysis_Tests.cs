using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.ServiceTimes;
using BreakfastProvider.Tests.Component.Shared.Models.ServiceTimes;
using BreakfastProvider.Tests.Component.TUnit.Infrastructure;
using Kronikol.TUnit;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.ServiceTimes;

public class ServiceTime_Analysis_Tests : BaseFixture
{
    private readonly PublishOrderServedEventSteps _publishSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;

    public ServiceTime_Analysis_Tests()
    {
        _publishSteps = Get<PublishOrderServedEventSteps>();
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

    [Test]
    [HappyPath]
    public async Task Consuming_an_order_served_event_should_trigger_downstream_processing()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

        // Given an order served event
        _publishSteps.Request = new TestOrderServedRequest
        {
            OrderId = Guid.NewGuid(),
            ItemType = "Pancakes",
            WaitSeconds = 245.5m
        };

        // When the event is published to Kafka (consumed by BreakfastProvider → ClickHouse + gRPC + HTTP)
        await _publishSteps.PublishEvent();

        // Then the order ID should be generated
        await _publishSteps.OrderId.Should().NotBeEqualTo(Guid.Empty);

        // And the kitchen service should have received the status request
        if (!Settings.RunAgainstExternalServiceUnderTest)
        {
            _downstreamSteps.AssertKitchenServiceReceivedStatusRequest(_publishSteps.OrderId);
        }
    }
}
