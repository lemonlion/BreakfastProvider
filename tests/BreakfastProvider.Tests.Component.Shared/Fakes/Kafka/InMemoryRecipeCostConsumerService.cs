using System.Collections.Concurrent;
using System.Text.Json;
using BreakfastProvider.Api;
using BreakfastProvider.Api.Reporting;
using BreakfastProvider.Tests.Component.Shared.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Kronikol.Tracking;

namespace BreakfastProvider.Tests.Component.Shared.Fakes.Kafka;

public class InMemoryRecipeCostConsumerService(
    ConsumedKafkaMessageStore consumedStore,
    IServiceScopeFactory scopeFactory,
    [FromKeyedServices("Kafka")] MessageTracker messageTracker,
    ILogger<InMemoryRecipeCostConsumerService> logger) : IHostedService
{
    private static readonly ConcurrentDictionary<Guid, byte> ProcessedCalculations = new();

    private const string EventTypeName = "RecipeCostCalculatedEvent";
    private const string TopicName = "breakfast_recipe_costs";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Task StartAsync(CancellationToken cancellationToken)
    {
        consumedStore.MessageStored += HandleMessage;
        logger.LogInformation("In-memory recipe cost Kafka consumer subscribed for {EventType}", EventTypeName);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        consumedStore.MessageStored -= HandleMessage;
        return Task.CompletedTask;
    }

    private void HandleMessage(string eventType, string key, string json)
    {
        if (!string.Equals(eventType, EventTypeName, StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            var message = JsonSerializer.Deserialize<RecipeCostMessage>(json, JsonOptions);
            if (message is null) return;

            // Guard: multiple WebApplicationFactory instances share the same global
            // ConsumedKafkaMessageStore, so each factory's subscriber receives ALL
            // messages. Dedup by CalculationId ensures exactly-once processing.
            if (!ProcessedCalculations.TryAdd(message.CalculationId, 0))
                return;

            messageTracker.TrackConsumeEvent(
                protocol: "Consume (Kafka)",
                consumerName: Documentation.ServiceNames.BreakfastProvider,
                sourceUri: new Uri($"kafka:///{TopicName}"),
                payload: message);

            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IRecipeCostAnalysisService>();

            service.ProcessCostCalculationAsync(
                message.CalculationId,
                message.RecipeName,
                message.Ingredients,
                message.TotalCost,
                message.Currency,
                message.CalculatedAt).GetAwaiter().GetResult();

            logger.LogInformation("In-memory consumer processed {EventType} for calculation {CalculationId}",
                EventTypeName, message.CalculationId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process {EventType} message", EventTypeName);
        }
    }

    private class RecipeCostMessage
    {
        public Guid CalculationId { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public List<string> Ingredients { get; set; } = [];
        public decimal TotalCost { get; set; }
        public string Currency { get; set; } = "GBP";
        public DateTime CalculatedAt { get; set; }
    }
}
