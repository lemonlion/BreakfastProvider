using System.Text.Json;
using BreakfastProvider.Api;
using BreakfastProvider.Api.Reporting;
using BreakfastProvider.Tests.Component.Shared.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TestTrackingDiagrams.Tracking;

namespace BreakfastProvider.Tests.Component.Shared.Fakes.EventHub;

/// <summary>
/// In-memory replacement for <see cref="EventHubEquipmentAlertConsumerService"/> that
/// subscribes to <see cref="ConsumedEventHubMessageStore.MessageStored"/> and
/// processes EquipmentAlertEvent messages synchronously within the HTTP request
/// context where they were produced.
///
/// The <see cref="MessageTracker.IsCurrentRequestFromMyHost"/> guard ensures
/// that only the consumer from the host that produced the message processes
/// it — preventing duplicate consume arrows when multiple
/// <see cref="WebApplicationFactory"/> instances share the same global
/// <see cref="ConsumedEventHubMessageStore"/>.
/// </summary>
public class InMemoryEventHubEquipmentAlertConsumerService : IHostedService
{
    private readonly ConsumedEventHubMessageStore _consumedStore;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MessageTracker _messageTracker;
    private readonly ILogger<InMemoryEventHubEquipmentAlertConsumerService> _logger;

    private const string EventTypeName = "EquipmentAlertEvent";
    private const string EventHubName = "breakfast-equipment-alerts";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public InMemoryEventHubEquipmentAlertConsumerService(
        ConsumedEventHubMessageStore consumedStore,
        IServiceScopeFactory scopeFactory,
        [FromKeyedServices("EventHub")] MessageTracker messageTracker,
        ILogger<InMemoryEventHubEquipmentAlertConsumerService> logger)
    {
        _consumedStore = consumedStore;
        _scopeFactory = scopeFactory;
        _messageTracker = messageTracker;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _consumedStore.MessageStored += HandleMessage;
        _logger.LogInformation("In-memory Event Hub equipment alert consumer subscribed for {EventType}", EventTypeName);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _consumedStore.MessageStored -= HandleMessage;
        return Task.CompletedTask;
    }

    private void HandleMessage(string eventType, string json)
    {
        if (!string.Equals(eventType, EventTypeName, StringComparison.OrdinalIgnoreCase))
            return;

        if (!_messageTracker.IsCurrentRequestFromMyHost())
            return;

        try
        {
            var message = JsonSerializer.Deserialize<EquipmentAlertMessage>(json, JsonOptions);
            if (message is null) return;

            _messageTracker.TrackConsumeEvent(
                protocol: "Consume (Event Hub)",
                consumerName: Documentation.ServiceNames.BreakfastProvider,
                sourceUri: new Uri($"eventhub:///{EventHubName}"),
                payload: message);

            using var scope = _scopeFactory.CreateScope();
            var ingester = scope.ServiceProvider.GetRequiredService<IReportingIngester>();

            ingester.IngestEquipmentAlertAsync(
                message.AlertId,
                message.BatchId,
                message.EquipmentName,
                message.AlertType,
                message.AlertedAt).GetAwaiter().GetResult();

            _logger.LogInformation("In-memory consumer ingested {EventType} for alert {AlertId}",
                EventTypeName, message.AlertId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process {EventType} message", EventTypeName);
        }
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
