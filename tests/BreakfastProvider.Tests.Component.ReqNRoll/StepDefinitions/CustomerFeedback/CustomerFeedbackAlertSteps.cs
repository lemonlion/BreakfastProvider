using BreakfastProvider.Tests.Component.Shared.Common.CustomerFeedback;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Models.CustomerFeedback;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.CustomerFeedback;

[Binding]
public class CustomerFeedbackAlertSteps(
    PublishCustomerFeedbackEventSteps publishSteps,
    DownstreamRequestSteps downstreamSteps)
{
    [Given("a customer feedback received event")]
    public void GivenACustomerFeedbackReceivedEvent()
    {
        publishSteps.Request = new TestCustomerFeedbackRequest
        {
            CustomerName = $"Customer-{Guid.NewGuid():N}",
            RecipeName = $"Recipe-{Guid.NewGuid():N}",
            Rating = 5,
            Comments = "Outstanding breakfast!"
        };
    }

    [When("the event is published to PubSub")]
    public async Task WhenTheEventIsPublishedToPubSub()
    {
        await publishSteps.PublishEvent();
    }

    [Then("the feedback ID should be generated")]
    public void ThenTheFeedbackIdShouldBeGenerated()
    {
        publishSteps.FeedbackId.Should().NotBe(Guid.Empty);
    }

    [Then("the supplier service should have received the feedback")]
    public void ThenTheSupplierServiceShouldHaveReceivedTheFeedback()
    {
        if (AppManager.Settings.RunAgainstExternalServiceUnderTest) return;
        downstreamSteps.AssertSupplierServiceReceivedFeedbackRequest();
    }
}
