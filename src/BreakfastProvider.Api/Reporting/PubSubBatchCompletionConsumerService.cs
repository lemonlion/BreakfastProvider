using System.Text.Json;
using BreakfastProvider.Api.Configuration;
using Google.Cloud.PubSub.V1;
using Microsoft.Extensions.Options;

namespace BreakfastProvider.Api.Reporting;

/// <summary>
/// Background service that consumes <c>PancakeBatchCompletedEvent</c> messages from
/// Google Cloud Pub/Sub and feeds them into the reporting database via
/// <see cref="IReportingIngester"/>.
/// </summary>
public class PubSubBatchCompletionConsumerService(
    IOptions<PubSubConfig> pubSubOptions,
    IServiceScopeFactory scopeFactory,
    ILogger<PubSubBatchCompletionConsumerService> logger) : BackgroundService
{
    private const string EventTypeName = "PancakeBatchCompletedEvent";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = pubSubOptions.Value;

        if (!config.SubscriberConfigurations.TryGetValue(EventTypeName, out var subConfig))
        {
            logger.LogWarning("No Pub/Sub subscriber configuration found for {EventType}. Batch completion consumer will not start.",
                EventTypeName);
            return;
        }

        var subscriptionName = SubscriptionName.FromProjectSubscription(config.ProjectId, subConfig.SubscriptionId);

        var subscriber = await SubscriberClient.CreateAsync(subscriptionName);
        logger.LogInformation("Batch completion Pub/Sub consumer started on subscription {Subscription}",
            subConfig.SubscriptionId);

        await subscriber.StartAsync(async (message, cancellationToken) =>
        {
            try
            {
                var json = message.Data.ToStringUtf8();
                var batch = JsonSerializer.Deserialize<BatchCompletionMessage>(json, JsonOptions);
                if (batch is null) return SubscriberClient.Reply.Ack;

                using var scope = scopeFactory.CreateScope();
                var ingester = scope.ServiceProvider.GetRequiredService<IReportingIngester>();

                await ingester.IngestBatchCompletionAsync(
                    batch.BatchId,
                    "Pancakes",
                    batch.Ingredients,
                    batch.CompletedAt,
                    cancellationToken);

                logger.LogInformation("Ingested batch completion for {BatchId}", batch.BatchId);
                return SubscriberClient.Reply.Ack;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process {EventType} message", EventTypeName);
                return SubscriberClient.Reply.Nack;
            }
        });

        // Wait until cancellation
        try { await Task.Delay(Timeout.Infinite, stoppingToken); }
        catch (OperationCanceledException) { }

        await subscriber.StopAsync(CancellationToken.None);
    }

    private class BatchCompletionMessage
    {
        public Guid BatchId { get; set; }
        public List<string> Ingredients { get; set; } = [];
        public List<string> Toppings { get; set; } = [];
        public DateTime CompletedAt { get; set; }
    }
}
