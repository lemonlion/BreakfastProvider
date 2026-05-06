using System.Diagnostics;
using System.Net;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Api.Storage;
using BreakfastProvider.Api.Telemetry;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace BreakfastProvider.Api.Events.Outbox;

public class OutboxProcessor(
    ICosmosRepository<OutboxMessage> outboxRepository,
    IEnumerable<IOutboxDispatcher> dispatchers,
    IOptions<OutboxConfig> config,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!config.Value.IsEnabled)
        {
            logger.LogInformation("OutboxProcessor is disabled via configuration");
            return;
        }

        var pollingInterval = TimeSpan.FromSeconds(config.Value.PollingIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing outbox messages");
            }

            try
            {
                await Task.Delay(pollingInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("OutboxProcessor.ProcessPendingMessages");

        var allPending = await outboxRepository.QueryAsync(
            m => m.Status == OutboxMessageStatus.Pending, cancellationToken);

        var pendingMessages = allPending.Take(config.Value.BatchSize).ToList();
        activity?.SetTag("outbox.batch_size", pendingMessages.Count);

        foreach (var message in pendingMessages)
        {
            var dispatcher = dispatchers.FirstOrDefault(d =>
                d.Destination.Equals(message.Destination, StringComparison.OrdinalIgnoreCase));

            if (dispatcher is null)
            {
                logger.LogWarning("No dispatcher found for destination {Destination}, message {MessageId}",
                    message.Destination, message.Id);
                continue;
            }

            // Claim the message with optimistic concurrency — prevents other processors from touching it.
            if (!await TryClaimMessage(message, cancellationToken))
                continue;

            await DispatchWithRetries(message, dispatcher, cancellationToken);
        }
    }

    private async Task<bool> TryClaimMessage(OutboxMessage message, CancellationToken cancellationToken)
    {
        try
        {
            message.Status = OutboxMessageStatus.Processing;
            var updated = await outboxRepository.ReplaceAsync(message, message.Id, message.PartitionKey, message.ETag, cancellationToken);
            message.ETag = updated.ETag;
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
        {
            logger.LogDebug("Outbox message {MessageId} was claimed by another processor, skipping", message.Id);
            return false;
        }
    }

    private async Task DispatchWithRetries(OutboxMessage message, IOutboxDispatcher dispatcher, CancellationToken cancellationToken)
    {
        var maxRetries = config.Value.MaxRetryCount;

        while (message.RetryCount < maxRetries)
        {
            using var dispatchActivity = DiagnosticsConfig.ActivitySource.StartActivity("OutboxProcessor.DispatchMessage");
            dispatchActivity?.SetTag("outbox.message_id", message.Id);
            dispatchActivity?.SetTag("outbox.event_type", message.EventType);
            dispatchActivity?.SetTag("outbox.destination", message.Destination);
            dispatchActivity?.SetTag("outbox.retry_count", message.RetryCount);

            try
            {
                await dispatcher.DispatchAsync(message, cancellationToken);

                message.Status = OutboxMessageStatus.Processed;
                message.ProcessedAt = DateTime.UtcNow;
                await outboxRepository.ReplaceAsync(message, message.Id, message.PartitionKey, message.ETag, cancellationToken);

                DiagnosticsConfig.OutboxMessagesDispatched.Add(1,
                    new KeyValuePair<string, object?>("outbox.destination", message.Destination));

                logger.LogInformation("Successfully dispatched outbox message {MessageId} ({EventType}) to {Destination}",
                    message.Id, message.EventType, message.Destination);
                return;
            }
            catch (CosmosException) { throw; }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.ErrorMessage = ex.Message;
                dispatchActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);

                if (message.RetryCount >= maxRetries)
                {
                    message.Status = OutboxMessageStatus.Failed;
                    await outboxRepository.ReplaceAsync(message, message.Id, message.PartitionKey, message.ETag, cancellationToken);

                    DiagnosticsConfig.OutboxMessagesFailed.Add(1,
                        new KeyValuePair<string, object?>("outbox.destination", message.Destination),
                        new KeyValuePair<string, object?>("outbox.event_type", message.EventType));

                    logger.LogError(ex, "Outbox message {MessageId} ({EventType}) failed after {RetryCount} retries",
                        message.Id, message.EventType, message.RetryCount);
                    return;
                }

                // Save retry progress (message stays "Processing" — still claimed by this processor)
                var updated = await outboxRepository.ReplaceAsync(message, message.Id, message.PartitionKey, message.ETag, cancellationToken);
                message.ETag = updated.ETag;

                logger.LogWarning(ex, "Outbox message {MessageId} ({EventType}) dispatch failed, retry {RetryCount}/{MaxRetries}",
                    message.Id, message.EventType, message.RetryCount, maxRetries);
            }
        }
    }
}
