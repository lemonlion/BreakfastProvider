using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.CustomerFeedback;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Models.CustomerFeedback;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.CustomerFeedback;

[Binding]
public class CustomerFeedbackAlertSteps(
    PostCustomerFeedbackSteps postSteps,
    DownstreamRequestSteps downstreamSteps)
{
    [Given("a valid customer feedback request")]
    public void GivenAValidCustomerFeedbackRequest()
    {
        postSteps.Request = new TestCustomerFeedbackRequest
        {
            CustomerName = $"Customer-{Guid.NewGuid():N}",
            RecipeName = $"Recipe-{Guid.NewGuid():N}",
            Rating = 5,
            Comments = "Outstanding breakfast!"
        };
    }

    [When("the customer feedback is submitted")]
    public async Task WhenTheCustomerFeedbackIsSubmitted()
    {
        await postSteps.Send();
    }

    [Then("the feedback response should be accepted")]
    public async Task ThenTheFeedbackResponseShouldBeAccepted()
    {
        postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Accepted);
        await postSteps.ParseResponse();
        postSteps.Response!.FeedbackId.Should().NotBe(Guid.Empty);
    }

    [Then("the supplier service should have received the feedback")]
    public async Task ThenTheSupplierServiceShouldHaveReceivedTheFeedback()
    {
        if (AppManager.Settings.RunAgainstExternalServiceUnderTest) return;
        await Task.Delay(500); // Allow async consumer processing
        downstreamSteps.AssertSupplierServiceReceivedFeedbackRequest();
    }
}
