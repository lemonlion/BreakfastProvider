using System.Collections.Concurrent;
using System.Text.Json;
using BreakfastProvider.Api;
using BreakfastProvider.Api.Reporting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Kronikol.Tracking;

namespace BreakfastProvider.Tests.Component.Shared.Fakes.Kafka;

public class InMemoryOrderServedConsumerService(
    ConsumedKafkaMessageStore consumedStore,
    IServiceScopeFactory scopeFactory,
    [FromKeyedServices("Kafka")] MessageTracker messageTracker,
    ILogger<InMemoryOrderServedConsumerService> logger) : IHostedService
{
    private static readonly ConcurrentDictionary<Guid, byte> ProcessedServices = new();

    private const string EventTypeName = "OrderServedEvent";
    private const string TopicName = "breakfast_orders_served";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Task StartAsync(CancellationToken cancellationToken)
    {
        consumedStore.MessageStored += HandleMessage;
        logger.LogInformation("In-memory order served Kafka consumer subscribed for {EventType}", EventTypeName);
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
            var message = JsonSerializer.Deserialize<OrderServedMessage>(json, JsonOptions);
            if (message is null) return;

            // Guard: multiple WebApplicationFactory instances share the same global
            // ConsumedKafkaMessageStore, so each factory's subscriber receives ALL
            // messages. Dedup by ServiceId ensures exactly-once processing.
            if (!ProcessedServices.TryAdd(message.ServiceId, 0))
                return;

            messageTracker.TrackConsumeEvent(
                protocol: "Consume (Kafka)",
                consumerName: Documentation.ServiceNames.BreakfastProvider,
                sourceUri: new Uri($"kafka:///{TopicName}"),
                payload: message);

            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IServiceTimeAnalysisService>();

            service.ProcessOrderServedAsync(
                message.ServiceId,
                message.OrderId,
                message.ItemType,
                message.WaitSeconds,
                message.ServedAt).GetAwaiter().GetResult();

            logger.LogInformation("In-memory consumer processed {EventType} for service {ServiceId}",
                EventTypeName, message.ServiceId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process {EventType} message", EventTypeName);
        }
    }

    private class OrderServedMessage
    {
        public Guid ServiceId { get; set; }
        public Guid OrderId { get; set; }
        public string ItemType { get; set; } = string.Empty;
        public decimal WaitSeconds { get; set; }
        public DateTime ServedAt { get; set; }
    }
}
