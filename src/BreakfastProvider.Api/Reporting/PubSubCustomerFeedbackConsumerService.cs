using System.Text.Json;
using BreakfastProvider.Api.Configuration;
using Google.Api.Gax;
using Google.Api.Gax.Grpc;
using Google.Cloud.PubSub.V1;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace BreakfastProvider.Api.Reporting;

/// <summary>
/// Background service that consumes <c>CustomerFeedbackReceivedEvent</c> messages from
/// Google Cloud Pub/Sub, stores them in MongoDB, sends a notification, and calls the Supplier Service.
/// </summary>
public class PubSubCustomerFeedbackConsumerService(
    IOptions<PubSubConfig> pubSubOptions,
    IServiceScopeFactory scopeFactory,
    ILogger<PubSubCustomerFeedbackConsumerService> logger) : BackgroundService
{
    private const string EventTypeName = "CustomerFeedbackReceivedEvent";

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
            logger.LogInformation("Pub/Sub ProjectId is not configured. Customer feedback consumer will not start.");
            return;
        }

        if (!config.SubscriberConfigurations.TryGetValue(EventTypeName, out var subConfig))
        {
            logger.LogWarning("No Pub/Sub subscriber configuration found for {EventType}. Customer feedback consumer will not start.",
                EventTypeName);
            return;
        }

        var subscriptionName = SubscriptionName.FromProjectSubscription(config.ProjectId, subConfig.SubscriptionId);
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

        logger.LogInformation("Customer feedback Pub/Sub consumer started (streaming) on subscription {Subscription}",
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

        logger.LogInformation("Customer feedback Pub/Sub consumer started (pull) on subscription {Subscription}",
            subscriptionName.SubscriptionId);

        var pullCallSettings = CallSettings.FromExpiration(Expiration.FromTimeout(TimeSpan.FromSeconds(5)));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await apiClient.PullAsync(subscriptionName, maxMessages: 10, pullCallSettings);

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
            catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded) { }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error during Pub/Sub pull, retrying...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ProcessMessageAsync(string json, CancellationToken cancellationToken)
    {
        var feedback = JsonSerializer.Deserialize<CustomerFeedbackMessage>(json, JsonOptions);
        if (feedback is null) return;

        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ICustomerFeedbackAlertService>();

        await service.ProcessFeedbackAsync(
            feedback.FeedbackId,
            feedback.CustomerName,
            feedback.RecipeName,
            feedback.Rating,
            feedback.Comments,
            feedback.ReceivedAt,
            cancellationToken);

        logger.LogInformation("Processed customer feedback {FeedbackId} for recipe {RecipeName}", feedback.FeedbackId, feedback.RecipeName);
    }

    private class CustomerFeedbackMessage
    {
        public Guid FeedbackId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string RecipeName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comments { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; }
    }
}
