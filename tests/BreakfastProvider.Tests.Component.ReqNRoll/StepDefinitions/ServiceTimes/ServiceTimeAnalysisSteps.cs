using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.ServiceTimes;
using BreakfastProvider.Tests.Component.Shared.Models.ServiceTimes;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.ServiceTimes;

[Binding]
public class ServiceTimeAnalysisSteps(
    PublishOrderServedEventSteps publishSteps,
    DownstreamRequestSteps downstreamSteps)
{
    [Given("an order served event")]
    public void GivenAnOrderServedEvent()
    {
        publishSteps.Request = new TestOrderServedRequest
        {
            OrderId = Guid.NewGuid(),
            ItemType = "Pancakes",
            WaitSeconds = 245.5m
        };
    }

    [When("the order served event is published to Kafka")]
    public async Task WhenTheOrderServedEventIsPublishedToKafka()
    {
        await publishSteps.PublishEvent();
    }

    [Then("the order ID should be generated")]
    public void ThenTheOrderIdShouldBeGenerated()
    {
        publishSteps.OrderId.Should().NotBe(Guid.Empty);
    }

    [Then("the kitchen service should have received the status request")]
    public void ThenTheKitchenServiceShouldHaveReceivedTheStatusRequest()
    {
        if (AppManager.Settings.RunAgainstExternalServiceUnderTest) return;
        downstreamSteps.AssertKitchenServiceReceivedStatusRequest(publishSteps.OrderId);
    }
}
