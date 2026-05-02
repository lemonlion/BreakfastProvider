using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using BreakfastProvider.Api.Configuration;

namespace BreakfastProvider.Api.Reporting;

public class EventHubEquipmentAlertConsumerService(
    IOptions<EventHubConfig> config,
    IServiceScopeFactory scopeFactory,
    ILogger<EventHubEquipmentAlertConsumerService> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var cfg = config.Value;
        if (string.IsNullOrWhiteSpace(cfg.ConnectionString))
        {
            logger.LogWarning("Event Hub connection string is not configured — equipment alert consumer will not start");
            return;
        }

        if (string.IsNullOrWhiteSpace(cfg.EventHubName))
        {
            logger.LogWarning("Event Hub name is not configured — equipment alert consumer will not start");
            return;
        }

        var blobContainerClient = new BlobContainerClient(cfg.BlobStorageConnectionString, cfg.BlobContainerName);
        await blobContainerClient.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

        var processor = new EventProcessorClient(
            blobContainerClient,
            cfg.ConsumerGroup,
            cfg.ConnectionString,
            cfg.EventHubName);

        processor.ProcessEventAsync += async args =>
        {
            try
            {
                var json = args.Data.EventBody.ToString();
                var message = JsonSerializer.Deserialize<EquipmentAlertMessage>(json, JsonOptions);
                if (message is null) return;

                using var scope = scopeFactory.CreateScope();
                var ingester = scope.ServiceProvider.GetRequiredService<IReportingIngester>();

                await ingester.IngestEquipmentAlertAsync(
                    message.AlertId,
                    message.BatchId,
                    message.EquipmentName,
                    message.AlertType,
                    message.AlertedAt,
                    stoppingToken);

                logger.LogInformation("Ingested equipment alert {AlertId} for batch {BatchId}",
                    message.AlertId, message.BatchId);

                await args.UpdateCheckpointAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process equipment alert event");
            }
        };

        processor.ProcessErrorAsync += args =>
        {
            logger.LogError(args.Exception, "Error in Event Hub processor for partition {PartitionId}",
                args.PartitionId);
            return Task.CompletedTask;
        };

        logger.LogInformation("Starting Event Hub equipment alert consumer for {EventHubName}", cfg.EventHubName);

        await processor.StartProcessingAsync(stoppingToken);

        try { await Task.Delay(Timeout.Infinite, stoppingToken); }
        catch (OperationCanceledException) { }

        await processor.StopProcessingAsync(CancellationToken.None);
    }

    private class EquipmentAlertMessage
    {
        public Guid AlertId { get; set; }
        public Guid BatchId { get; set; }
        public string EquipmentName { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
        public DateTime AlertedAt { get; set; }
    }
}
