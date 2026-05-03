using System.Text.Json;
using BreakfastProvider.Api.Configuration;
using Google.Api.Gax;
using Google.Api.Gax.Grpc;
using Google.Cloud.PubSub.V1;
using Grpc.Core;
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

        if (string.IsNullOrWhiteSpace(config.ProjectId))
        {
            logger.LogInformation("Pub/Sub ProjectId is not configured. Batch completion consumer will not start.");
            return;
        }

        if (!config.SubscriberConfigurations.TryGetValue(EventTypeName, out var subConfig))
        {
            logger.LogWarning("No Pub/Sub subscriber configuration found for {EventType}. Batch completion consumer will not start.",
                EventTypeName);
            return;
        }

        var subscriptionName = SubscriptionName.FromProjectSubscription(config.ProjectId, subConfig.SubscriptionId);

        // The PubSub emulator's StreamingPull (gRPC bidirectional stream) does not reliably
        // deliver messages. When running against the emulator, use simple unary Pull RPCs
        // in a polling loop instead.
        var isEmulator = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PUBSUB_EMULATOR_HOST"));

        if (isEmulator)
        {
            await RunPullLoopAsync(subscriptionName, stoppingToken);
        }
        else
        {
            await RunStreamingPullAsync(subscriptionName, stoppingToken);
        }
    }

    private async Task RunStreamingPullAsync(SubscriptionName subscriptionName, CancellationToken stoppingToken)
    {
        var subscriber = await new SubscriberClientBuilder
        {
            SubscriptionName = subscriptionName,
            EmulatorDetection = EmulatorDetection.EmulatorOrProduction
        }.BuildAsync(stoppingToken);

        logger.LogInformation("Batch completion Pub/Sub consumer started (streaming) on subscription {Subscription}",
            subscriptionName.SubscriptionId);

        await subscriber.StartAsync(async (message, cancellationToken) =>
        {
            try
            {
                await ProcessMessageAsync(message.Data.ToStringUtf8(), cancellationToken);
                return SubscriberClient.Reply.Ack;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process {EventType} message", EventTypeName);
                return SubscriberClient.Reply.Nack;
            }
        });

        try { await Task.Delay(Timeout.Infinite, stoppingToken); }
        catch (OperationCanceledException) { }

        await subscriber.StopAsync(CancellationToken.None);
    }

    private async Task RunPullLoopAsync(SubscriptionName subscriptionName, CancellationToken stoppingToken)
    {
        var apiClient = await new SubscriberServiceApiClientBuilder
        {
            EmulatorDetection = EmulatorDetection.EmulatorOrProduction
        }.BuildAsync(stoppingToken);

        logger.LogInformation("Batch completion Pub/Sub consumer started (pull) on subscription {Subscription}",
            subscriptionName.SubscriptionId);

        // Use a short gRPC deadline on each Pull so the emulator's long-poll doesn't
        // block indefinitely when no messages exist yet. The emulator may not wake a
        // waiting Pull when new messages arrive after the call was issued.
        var pullCallSettings = CallSettings.FromExpiration(Expiration.FromTimeout(TimeSpan.FromSeconds(5)));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await apiClient.PullAsync(
                    subscriptionName,
                    maxMessages: 10,
                    pullCallSettings);

                if (response.ReceivedMessages.Count == 0)
                    continue;

                var ackIds = new List<string>();
                foreach (var received in response.ReceivedMessages)
                {
                    try
                    {
                        await ProcessMessageAsync(received.Message.Data.ToStringUtf8(), stoppingToken);
                        ackIds.Add(received.AckId);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to process {EventType} message", EventTypeName);
                    }
                }

                if (ackIds.Count > 0)
                {
                    await apiClient.AcknowledgeAsync(subscriptionName, ackIds, stoppingToken);
                }
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
            {
                // Normal timeout — no messages available yet, loop around immediately
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error during Pub/Sub pull, retrying...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ProcessMessageAsync(string json, CancellationToken cancellationToken)
    {
        var batch = JsonSerializer.Deserialize<BatchCompletionMessage>(json, JsonOptions);
        if (batch is null) return;

        using var scope = scopeFactory.CreateScope();
        var ingester = scope.ServiceProvider.GetRequiredService<IReportingIngester>();

        await ingester.IngestBatchCompletionAsync(
            batch.BatchId,
            "Pancakes",
            batch.Ingredients,
            batch.CompletedAt,
            cancellationToken);

        logger.LogInformation("Ingested batch completion for {BatchId}", batch.BatchId);
    }

    private class BatchCompletionMessage
    {
        public Guid BatchId { get; set; }
        public List<string> Ingredients { get; set; } = [];
        public List<string> Toppings { get; set; } = [];
        public DateTime CompletedAt { get; set; }
    }
}
