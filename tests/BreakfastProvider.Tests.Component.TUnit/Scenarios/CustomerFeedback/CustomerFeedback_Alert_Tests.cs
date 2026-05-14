using BreakfastProvider.Tests.Component.Shared.Common.CustomerFeedback;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Models.CustomerFeedback;
using BreakfastProvider.Tests.Component.TUnit.Infrastructure;
using TestTrackingDiagrams.TUnit;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.CustomerFeedback;

public class CustomerFeedback_Alert_Tests : BaseFixture
{
    private readonly PublishCustomerFeedbackEventSteps _publishSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;

    public CustomerFeedback_Alert_Tests()
    {
        _publishSteps = Get<PublishCustomerFeedbackEventSteps>();
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

    [Test]
    [HappyPath]
    public async Task Consuming_customer_feedback_event_should_trigger_downstream_processing()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

        // Given a customer feedback received event
        _publishSteps.Request = new TestCustomerFeedbackRequest
        {
            CustomerName = $"Customer-{Guid.NewGuid():N}",
            RecipeName = $"Recipe-{Guid.NewGuid():N}",
            Rating = 5,
            Comments = "Outstanding breakfast!"
        };

        // When the event is published to Pub/Sub (consumed by BreakfastProvider → MongoDB + gRPC + HTTP)
        await _publishSteps.PublishEvent();

        // Then the feedback ID should be generated
        await _publishSteps.FeedbackId.Should().NotBeEqualTo(Guid.Empty);

        // And the supplier service should have received the feedback notification
        if (!Settings.RunAgainstExternalServiceUnderTest)
        {
            _downstreamSteps.AssertSupplierServiceReceivedFeedbackRequest();
        }
    }
}
