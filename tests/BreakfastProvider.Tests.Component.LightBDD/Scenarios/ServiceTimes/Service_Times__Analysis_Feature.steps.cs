using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.ServiceTimes;
using BreakfastProvider.Tests.Component.Shared.Models.ServiceTimes;
using BreakfastProvider.Tests.Component.LightBDD.Util;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.ServiceTimes;

public partial class Service_Times__Analysis_Feature : BaseFixture
{
    private readonly PublishOrderServedEventSteps _publishSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;

    public Service_Times__Analysis_Feature()
    {
        _publishSteps = Get<PublishOrderServedEventSteps>();
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

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

    [SkipStepIf(nameof(Settings.RunAgainstExternalServiceUnderTest), DownstreamFakeRequestStoreIsUnavailableInPostDeploymentEnvironments)]
    private async Task The_kitchen_service_should_have_received_the_status_request()
    {
        _downstreamSteps.AssertKitchenServiceReceivedStatusRequest(_publishSteps.OrderId);
        await Task.CompletedTask;
    }
}
