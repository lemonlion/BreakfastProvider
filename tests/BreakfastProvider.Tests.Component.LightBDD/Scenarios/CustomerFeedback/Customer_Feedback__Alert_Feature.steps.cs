using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.CustomerFeedback;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Models.CustomerFeedback;
using BreakfastProvider.Tests.Component.LightBDD.Util;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.CustomerFeedback;

public partial class Customer_Feedback__Alert_Feature : BaseFixture
{
    private readonly PostCustomerFeedbackSteps _postSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;

    public Customer_Feedback__Alert_Feature()
    {
        _postSteps = Get<PostCustomerFeedbackSteps>();
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

    private async Task A_valid_customer_feedback_request()
    {
        _postSteps.Request = new TestCustomerFeedbackRequest
        {
            CustomerName = $"Customer-{Guid.NewGuid():N}",
            RecipeName = $"Recipe-{Guid.NewGuid():N}",
            Rating = 5,
            Comments = "Outstanding breakfast!"
        };
        await Task.CompletedTask;
    }

    private async Task The_feedback_is_submitted() => await _postSteps.Send();

    private async Task The_response_should_be_accepted()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Accepted);
        await _postSteps.ParseResponse();
        _postSteps.Response!.FeedbackId.Should().NotBe(Guid.Empty);
    }

    private async Task The_supplier_service_should_have_received_the_feedback()
    {
        await Task.Delay(500); // Allow async consumer processing
        _downstreamSteps.AssertSupplierServiceReceivedFeedbackRequest();
    }
}
