using System.Text.Json;
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
        var eventHubName = config.Value.EventHubName;
        if (string.IsNullOrWhiteSpace(eventHubName))
        {
            logger.LogWarning("Event Hub name is not configured — equipment alert consumer will not start");
            return;
        }

        logger.LogInformation("Starting Event Hub equipment alert consumer for {EventHubName}", eventHubName);

        // In production this would use EventProcessorClient to consume from the Event Hub
        // partition, checkpoint via Blob Storage, and process events in a loop.
        // For component tests, InMemoryEventHubEquipmentAlertConsumerService replaces this.
        await Task.Delay(Timeout.Infinite, stoppingToken);
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
