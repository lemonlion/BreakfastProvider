using System.Text.Json;
using BreakfastProvider.Api;
using BreakfastProvider.Api.Reporting;
using BreakfastProvider.Tests.Component.Shared.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TestTrackingDiagrams.Tracking;

namespace BreakfastProvider.Tests.Component.Shared.Fakes.Kafka;

/// <summary>
/// In-memory replacement for <see cref="ReportingKafkaConsumerService"/> that
/// subscribes to <see cref="ConsumedKafkaMessageStore.MessageStored"/> and
/// processes RecipeLogEvent messages synchronously within the HTTP request
/// context where they were produced.
///
/// Because <see cref="InMemoryProducer{TKey,TValue}"/> fires during the
/// API request pipeline, the subscriber runs in the same ASP.NET Core
/// request context — allowing <see cref="MessageTracker"/> to attribute
/// the "Consume (Kafka)" interaction to the correct test's PlantUML diagram.
///
/// The <see cref="MessageTracker.IsCurrentRequestFromMyHost"/> guard ensures
/// that only the consumer from the host that produced the message processes
/// it — preventing duplicate consume arrows when multiple
/// <see cref="WebApplicationFactory"/> instances share the same global
/// <see cref="ConsumedKafkaMessageStore"/>.
/// </summary>
public class InMemoryReportingKafkaConsumerService(
    ConsumedKafkaMessageStore consumedStore,
    IServiceScopeFactory scopeFactory,
    [FromKeyedServices("Kafka")] MessageTracker messageTracker,
    ILogger<InMemoryReportingKafkaConsumerService> logger) : IHostedService
{
    private const string EventTypeName = "RecipeLogEvent";
    private const string TopicName = "breakfast_recipe_logs";
    private const string BrokerName = "Kafka Broker";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Task StartAsync(CancellationToken cancellationToken)
    {
        consumedStore.MessageStored += HandleMessage;
        logger.LogInformation("In-memory reporting Kafka consumer subscribed for {EventType}", EventTypeName);
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

        // Guard: only process if the current HttpContext belongs to OUR host.
        // Multiple WebApplicationFactory instances share the same global
        // ConsumedKafkaMessageStore, so each factory's subscriber receives ALL
        // messages. Without this check, N subscribers produce N duplicate
        // consume arrows in the diagram.
        if (!messageTracker.IsCurrentRequestFromMyHost())
            return;

        try
        {
            var message = JsonSerializer.Deserialize<RecipeLogMessage>(json, JsonOptions);
            if (message is null) return;

            // TrackConsumeEvent models broker → consumer delivery with the
            // payload on the delivery arrow and an "Ack" return arrow.
            messageTracker.TrackConsumeEvent(
                protocol: "Consume (Kafka)",
                consumerName: Documentation.ServiceNames.BreakfastProvider,
                sourceUri: new Uri($"kafka:///{TopicName}"),
                payload: message);

            using var scope = scopeFactory.CreateScope();
            var ingester = scope.ServiceProvider.GetRequiredService<IReportingIngester>();

            ingester.IngestRecipeLogAsync(
                message.OrderId,
                message.RecipeType,
                message.Ingredients,
                message.Toppings,
                message.LoggedAt).GetAwaiter().GetResult();

            logger.LogInformation("In-memory consumer ingested {EventType} for order {OrderId}",
                EventTypeName, message.OrderId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process {EventType} message", EventTypeName);
        }
    }

    private class RecipeLogMessage
    {
        public Guid OrderId { get; set; }
        public string RecipeType { get; set; } = string.Empty;
        public List<string> Ingredients { get; set; } = [];
        public List<string> Toppings { get; set; } = [];
        public DateTime LoggedAt { get; set; }
    }
}
