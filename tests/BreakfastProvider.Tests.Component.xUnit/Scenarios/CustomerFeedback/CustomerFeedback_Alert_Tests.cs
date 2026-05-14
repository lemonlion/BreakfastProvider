using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.CustomerFeedback;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Models.CustomerFeedback;
using BreakfastProvider.Tests.Component.xUnit.Infrastructure;
using TestTrackingDiagrams.xUnit3;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.CustomerFeedback;

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
    public async Task Submitting_customer_feedback_should_trigger_event_consumption_and_downstream_calls()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

        // Given a valid customer feedback request
        _postSteps.Request = new TestCustomerFeedbackRequest
        {
            CustomerName = $"Customer-{Guid.NewGuid():N}",
            RecipeName = $"Recipe-{Guid.NewGuid():N}",
            Rating = 5,
            Comments = "Outstanding breakfast!"
        };

        // When the feedback is submitted (triggers PubSub event → consumer → MongoDB + gRPC + HTTP)
        await _postSteps.Send();

        // Then the response should be accepted
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Accepted);
        await _postSteps.ParseResponse();
        _postSteps.Response!.FeedbackId.Should().NotBe(Guid.Empty);

        // And the supplier service should have received the feedback notification
        await Task.Delay(500); // Allow async consumer processing
        _downstreamSteps.AssertSupplierServiceReceivedFeedbackRequest();
    }
}
