using System.Text.Json;
using BreakfastProvider.Tests.Component.Shared.Fakes.Kafka;
using BreakfastProvider.Tests.Component.Shared.Models.RecipeCosts;
using Confluent.Kafka;
using TestTrackingDiagrams.Tracking;

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

        using (TestIdentityScope.Begin("RecipeCostTest", context.RequestId))
        {
            var message = new Message<string, string>
            {
                Key = CalculationId.ToString(),
                Value = JsonSerializer.Serialize(@event),
                Headers = new Headers
                {
                    { "ttd-test-name", System.Text.Encoding.UTF8.GetBytes("RecipeCostTest") },
                    { "ttd-test-id", System.Text.Encoding.UTF8.GetBytes(context.RequestId) }
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
