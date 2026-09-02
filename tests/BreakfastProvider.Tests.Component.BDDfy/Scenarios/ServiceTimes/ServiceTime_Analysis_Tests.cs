using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.ServiceTimes;
using BreakfastProvider.Tests.Component.Shared.Models.ServiceTimes;
using BreakfastProvider.Tests.Component.BDDfy.Infrastructure;

using TestStack.BDDfy;
using Kronikol.BDDfy.xUnit3;

namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.ServiceTimes;

public class ServiceTime_Analysis_Tests : BaseFixture
{
    private readonly PublishOrderServedEventSteps _publishSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;

    public ServiceTime_Analysis_Tests()
    {
        _publishSteps = Get<PublishOrderServedEventSteps>();
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

    [Fact]
    [HappyPath]
    public void Consuming_an_order_served_event_should_trigger_downstream_processing()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

        this.Given(x => x.An_order_served_event())
            .When(x => x.The_event_is_published_to_kafka())
            .Then(x => x.The_order_id_should_be_generated())
            .And(x => x.The_kitchen_service_should_have_received_the_status_request())
            .BDDfy();
    }

    #region Steps

    private async Task An_order_served_event()
    {
        _publishSteps.Request = new TestOrderServedRequest
        {
            OrderId = Guid.NewGuid(),
            ItemType = "Pancakes",
            WaitSeconds = 245.5m
        };
        await Task.CompletedTask;
    }

    private async Task The_event_is_published_to_kafka() => await _publishSteps.PublishEvent();

    private async Task The_order_id_should_be_generated()
    {
        _publishSteps.OrderId.Should().NotBe(Guid.Empty);
        await Task.CompletedTask;
    }

    private async Task The_kitchen_service_should_have_received_the_status_request()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;
        _downstreamSteps.AssertKitchenServiceReceivedStatusRequest(_publishSteps.OrderId);
        await Task.CompletedTask;
    }

    #endregion
}
