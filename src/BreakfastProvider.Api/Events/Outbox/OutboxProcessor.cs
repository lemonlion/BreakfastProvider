using System.Diagnostics;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Api.Storage;
using BreakfastProvider.Api.Telemetry;
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

            using var dispatchActivity = DiagnosticsConfig.ActivitySource.StartActivity("OutboxProcessor.DispatchMessage");
            dispatchActivity?.SetTag("outbox.message_id", message.Id);
            dispatchActivity?.SetTag("outbox.event_type", message.EventType);
            dispatchActivity?.SetTag("outbox.destination", message.Destination);

            try
            {
                await dispatcher.DispatchAsync(message, cancellationToken);

                message.Status = OutboxMessageStatus.Processed;
                message.ProcessedAt = DateTime.UtcNow;
                await outboxRepository.UpsertAsync(message, message.PartitionKey, cancellationToken);

                DiagnosticsConfig.OutboxMessagesDispatched.Add(1,
                    new KeyValuePair<string, object?>("outbox.destination", message.Destination));

                logger.LogInformation("Successfully dispatched outbox message {MessageId} ({EventType}) to {Destination}",
                    message.Id, message.EventType, message.Destination);
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.ErrorMessage = ex.Message;

                if (message.RetryCount >= config.Value.MaxRetryCount)
                {
                    message.Status = OutboxMessageStatus.Failed;

                    DiagnosticsConfig.OutboxMessagesFailed.Add(1,
                        new KeyValuePair<string, object?>("outbox.destination", message.Destination),
                        new KeyValuePair<string, object?>("outbox.event_type", message.EventType));

                    logger.LogError(ex, "Outbox message {MessageId} ({EventType}) failed after {RetryCount} retries",
                        message.Id, message.EventType, message.RetryCount);
                }
                else
                {
                    logger.LogWarning(ex, "Outbox message {MessageId} ({EventType}) dispatch failed, retry {RetryCount}/{MaxRetries}",
                        message.Id, message.EventType, message.RetryCount, config.Value.MaxRetryCount);
                }

                dispatchActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);

                await outboxRepository.UpsertAsync(message, message.PartitionKey, cancellationToken);
            }
        }
    }
}
