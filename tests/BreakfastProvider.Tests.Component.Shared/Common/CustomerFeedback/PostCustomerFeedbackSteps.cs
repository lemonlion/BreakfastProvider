using System.Text.Json;
using BreakfastProvider.Tests.Component.Shared.Fakes.HttpFakes;
using BreakfastProvider.Tests.Component.Shared.Fakes.PubSub;
using BreakfastProvider.Tests.Component.Shared.Models.CustomerFeedback;
using Kronikol.Tracking;

namespace BreakfastProvider.Tests.Component.Shared.Common.CustomerFeedback;

public class PublishCustomerFeedbackEventSteps(
    ConsumedPubSubMessageStore pubSubStore,
    RequestContext context)
{
    public TestCustomerFeedbackRequest Request { get; set; } = new();
    public Guid FeedbackId { get; private set; }

    public Task PublishEvent()
    {
        FeedbackId = Guid.NewGuid();

        var @event = new CustomerFeedbackReceivedEvent
        {
            FeedbackId = FeedbackId,
            CustomerName = Request.CustomerName!,
            RecipeName = Request.RecipeName!,
            Rating = Request.Rating,
            Comments = Request.Comments ?? string.Empty,
            ReceivedAt = DateTime.UtcNow
        };

        // The in-memory consumer handles the event synchronously inside these scopes, so the MongoDB / gRPC /
        // Supplier arrows are attributed to this test with no HTTP request in flight, and the Supplier fake
        // files its request under the request id the Then step looks it up by.
        var test = context.CurrentTest;
        using (TestIdentityScope.Begin(test.Name, test.Id))
        using (ComponentTestRequestIdScope.Begin(context.RequestId))
        {
            pubSubStore.Add(@event, "CustomerFeedbackReceivedEvent");
        }

        return Task.CompletedTask;
    }

    private class CustomerFeedbackReceivedEvent
    {
        public Guid FeedbackId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string RecipeName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comments { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; }
    }
}
