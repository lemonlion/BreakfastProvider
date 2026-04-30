using System.Text.Json;
using BreakfastProvider.Api;
using BreakfastProvider.Api.Reporting;
using BreakfastProvider.Tests.Component.Shared.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TestTrackingDiagrams.Tracking;

namespace BreakfastProvider.Tests.Component.Shared.Fakes.PubSub;

/// <summary>
/// In-memory replacement for <see cref="PubSubBatchCompletionConsumerService"/> that
/// subscribes to <see cref="ConsumedPubSubMessageStore.MessageStored"/> and
/// processes PancakeBatchCompletedEvent messages synchronously within the HTTP request
/// context where they were produced.
///
/// Because <see cref="InMemoryPubSubEventPublisher{T}"/> fires during the
/// API request pipeline, the subscriber runs in the same ASP.NET Core
/// request context — allowing <see cref="MessageTracker"/> to attribute
/// the "Consume (Pub/Sub)" interaction to the correct test's PlantUML diagram.
///
/// The <see cref="MessageTracker.IsCurrentRequestFromMyHost"/> guard ensures
/// that only the consumer from the host that produced the message processes
/// it — preventing duplicate consume arrows when multiple
/// <see cref="WebApplicationFactory"/> instances share the same global
/// <see cref="ConsumedPubSubMessageStore"/>.
/// </summary>
public class InMemoryPubSubBatchCompletionConsumerService : IHostedService
{
    private readonly ConsumedPubSubMessageStore _consumedStore;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MessageTracker _messageTracker;
    private readonly ILogger<InMemoryPubSubBatchCompletionConsumerService> _logger;

    private const string EventTypeName = "PancakeBatchCompletedEvent";
    private const string TopicName = "breakfast_batch_completions";
    private const string BrokerName = "Google Cloud Pub/Sub";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public InMemoryPubSubBatchCompletionConsumerService(
        ConsumedPubSubMessageStore consumedStore,
        IServiceScopeFactory scopeFactory,
        [FromKeyedServices("PubSub")] MessageTracker messageTracker,
        ILogger<InMemoryPubSubBatchCompletionConsumerService> logger)
    {
        _consumedStore = consumedStore;
        _scopeFactory = scopeFactory;
        _messageTracker = messageTracker;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _consumedStore.MessageStored += HandleMessage;
        _logger.LogInformation("In-memory batch completion Pub/Sub consumer subscribed for {EventType}", EventTypeName);
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

        // Guard: only process if the current HttpContext belongs to OUR host.
        // Multiple WebApplicationFactory instances share the same global
        // ConsumedPubSubMessageStore, so each factory's subscriber receives ALL
        // messages. Without this check, N subscribers produce N duplicate
        // consume arrows in the diagram.
        if (!_messageTracker.IsCurrentRequestFromMyHost())
            return;

        try
        {
            var message = JsonSerializer.Deserialize<BatchCompletionMessage>(json, JsonOptions);
            if (message is null) return;

            // TrackConsumeEvent models broker → consumer delivery with the
            // payload on the delivery arrow and an "Ack" return arrow.
            _messageTracker.TrackConsumeEvent(
                protocol: "Consume (Pub/Sub)",
                consumerName: Documentation.ServiceNames.BreakfastProvider,
                sourceUri: new Uri($"pubsub:///{TopicName}"),
                payload: message);

            using var scope = _scopeFactory.CreateScope();
            var ingester = scope.ServiceProvider.GetRequiredService<IReportingIngester>();

            ingester.IngestBatchCompletionAsync(
                message.BatchId,
                "Pancakes",
                message.Ingredients,
                message.CompletedAt).GetAwaiter().GetResult();

            _logger.LogInformation("In-memory consumer ingested {EventType} for batch {BatchId}",
                EventTypeName, message.BatchId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process {EventType} message", EventTypeName);
        }
    }

    private class BatchCompletionMessage
    {
        public Guid BatchId { get; set; }
        public List<string> Ingredients { get; set; } = [];
        public List<string> Toppings { get; set; } = [];
        public DateTime CompletedAt { get; set; }
    }
}
