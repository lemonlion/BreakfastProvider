using BreakfastProvider.Tests.Component.Shared.Common.CustomerFeedback;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Models.CustomerFeedback;
using BreakfastProvider.Tests.Component.BDDfy.Infrastructure;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;

namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.CustomerFeedback;

public class CustomerFeedback_Alert_Tests : BaseFixture
{
    private readonly PublishCustomerFeedbackEventSteps _publishSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;

    public CustomerFeedback_Alert_Tests()
    {
        _publishSteps = Get<PublishCustomerFeedbackEventSteps>();
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

    [Fact]
    [HappyPath]
    public void Consuming_customer_feedback_event_should_trigger_downstream_processing()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

        this.Given(x => x.A_customer_feedback_received_event())
            .When(x => x.The_event_is_published_to_pubsub())
            .Then(x => x.The_feedback_id_should_be_generated())
            .And(x => x.The_supplier_service_should_have_received_the_feedback())
            .BDDfy();
    }

    #region Steps

    private async Task A_customer_feedback_received_event()
    {
        _publishSteps.Request = new TestCustomerFeedbackRequest
        {
            CustomerName = $"Customer-{Guid.NewGuid():N}",
            RecipeName = $"Recipe-{Guid.NewGuid():N}",
            Rating = 5,
            Comments = "Outstanding breakfast!"
        };
        await Task.CompletedTask;
    }

    private async Task The_event_is_published_to_pubsub() => await _publishSteps.PublishEvent();

    private async Task The_feedback_id_should_be_generated()
    {
        _publishSteps.FeedbackId.Should().NotBe(Guid.Empty);
        await Task.CompletedTask;
    }

    private async Task The_supplier_service_should_have_received_the_feedback()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;
        _downstreamSteps.AssertSupplierServiceReceivedFeedbackRequest();
        await Task.CompletedTask;
    }

    #endregion
}
