using BreakfastProvider.Tests.Component.Shared.Common.CustomerFeedback;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Models.CustomerFeedback;
using BreakfastProvider.Tests.Component.LightBDD.Util;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.CustomerFeedback;

public partial class Customer_Feedback__Alert_Feature : BaseFixture
{
    private readonly PublishCustomerFeedbackEventSteps _publishSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;

    public Customer_Feedback__Alert_Feature()
    {
        _publishSteps = Get<PublishCustomerFeedbackEventSteps>();
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

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

    [SkipStepIf(nameof(Settings.RunAgainstExternalServiceUnderTest), DownstreamFakeRequestStoreIsUnavailableInPostDeploymentEnvironments)]
    private async Task The_supplier_service_should_have_received_the_feedback()
    {
        _downstreamSteps.AssertSupplierServiceReceivedFeedbackRequest();
        await Task.CompletedTask;
    }
}
