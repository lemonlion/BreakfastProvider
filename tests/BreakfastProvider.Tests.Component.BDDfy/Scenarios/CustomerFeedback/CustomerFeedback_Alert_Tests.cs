using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.CustomerFeedback;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Models.CustomerFeedback;
using BreakfastProvider.Tests.Component.BDDfy.Infrastructure;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;

namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.CustomerFeedback;

public class CustomerFeedback_Alert_Tests : BaseFixture
{
    private readonly PostCustomerFeedbackSteps _postSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;

    public CustomerFeedback_Alert_Tests()
    {
        _postSteps = Get<PostCustomerFeedbackSteps>();
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

    [Fact]
    [HappyPath]
    public void Submitting_customer_feedback_should_trigger_event_consumption_and_downstream_calls()
    {
        this.Given(x => x.A_valid_customer_feedback_request_is_prepared())
            .When(x => x.The_feedback_is_submitted())
            .Then(x => x.The_response_should_be_accepted())
            .And(x => x.The_supplier_service_should_have_received_the_feedback())
            .BDDfy();
    }

    #region Steps

    private async Task A_valid_customer_feedback_request_is_prepared()
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
        if (Settings.RunAgainstExternalServiceUnderTest) return;
        await Task.Delay(500); // Allow async consumer processing
        _downstreamSteps.AssertSupplierServiceReceivedFeedbackRequest();
    }

    #endregion
}
