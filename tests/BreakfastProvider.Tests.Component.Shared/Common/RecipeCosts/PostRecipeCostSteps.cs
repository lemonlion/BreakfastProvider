using System.Text.Json;
using BreakfastProvider.Tests.Component.Shared.Fakes.HttpFakes;
using BreakfastProvider.Tests.Component.Shared.Fakes.Kafka;
using BreakfastProvider.Tests.Component.Shared.Models.RecipeCosts;
using Confluent.Kafka;
using Kronikol.Constants;
using Kronikol.Tracking;

namespace BreakfastProvider.Tests.Component.Shared.Common.RecipeCosts;

public class PublishRecipeCostEventSteps(
    ConsumedKafkaMessageStore kafkaStore,
    RequestContext context)
{
    public TestRecipeCostRequest Request { get; set; } = new();
    public Guid CalculationId { get; private set; }

    public Task PublishEvent()
    {
        CalculationId = Guid.NewGuid();

        var @event = new RecipeCostCalculatedEvent
        {
            CalculationId = CalculationId,
            RecipeName = Request.RecipeName!,
            Ingredients = Request.Ingredients ?? [],
            TotalCost = Request.TotalCost,
            Currency = Request.Currency ?? "GBP",
            CalculatedAt = DateTime.UtcNow
        };

        // The in-memory consumer handles the message synchronously inside these scopes, so the BigQuery / gRPC /
        // Kitchen arrows are attributed to this test with no HTTP request in flight, and the Kitchen fake files
        // its request under the request id the Then step looks it up by.
        var test = context.CurrentTest;
        using (TestIdentityScope.Begin(test.Name, test.Id))
        using (ComponentTestRequestIdScope.Begin(context.RequestId))
        {
            var message = new Message<string, string>
            {
                Key = CalculationId.ToString(),
                Value = JsonSerializer.Serialize(@event),
                Headers = new Headers
                {
                    { TestTrackingMessageHeaders.TestName, System.Text.Encoding.UTF8.GetBytes(test.Name) },
                    { TestTrackingMessageHeaders.TestId, System.Text.Encoding.UTF8.GetBytes(test.Id) }
                }
            };

            kafkaStore.Add(message, "RecipeCostCalculatedEvent");
        }

        return Task.CompletedTask;
    }

    private class RecipeCostCalculatedEvent
    {
        public Guid CalculationId { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public List<string> Ingredients { get; set; } = [];
        public decimal TotalCost { get; set; }
        public string Currency { get; set; } = "GBP";
        public DateTime CalculatedAt { get; set; }
    }
}
