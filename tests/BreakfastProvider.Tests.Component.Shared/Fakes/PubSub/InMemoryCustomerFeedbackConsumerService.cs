using System.Text.Json;
using BreakfastProvider.Api;
using BreakfastProvider.Api.Reporting;
using BreakfastProvider.Tests.Component.Shared.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TestTrackingDiagrams.Tracking;

namespace BreakfastProvider.Tests.Component.Shared.Fakes.PubSub;

public class InMemoryCustomerFeedbackConsumerService : IHostedService
{
    private readonly ConsumedPubSubMessageStore _consumedStore;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MessageTracker _messageTracker;
    private readonly ILogger<InMemoryCustomerFeedbackConsumerService> _logger;

    private const string EventTypeName = "CustomerFeedbackReceivedEvent";
    private const string TopicName = "customer_feedback_received";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public InMemoryCustomerFeedbackConsumerService(
        ConsumedPubSubMessageStore consumedStore,
        IServiceScopeFactory scopeFactory,
        [FromKeyedServices("PubSub")] MessageTracker messageTracker,
        ILogger<InMemoryCustomerFeedbackConsumerService> logger)
    {
        _consumedStore = consumedStore;
        _scopeFactory = scopeFactory;
        _messageTracker = messageTracker;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _consumedStore.MessageStored += HandleMessage;
        _logger.LogInformation("In-memory customer feedback Pub/Sub consumer subscribed for {EventType}", EventTypeName);
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
            var message = JsonSerializer.Deserialize<CustomerFeedbackMessage>(json, JsonOptions);
            if (message is null) return;

            _messageTracker.TrackConsumeEvent(
                protocol: "Consume (Pub/Sub)",
                consumerName: Documentation.ServiceNames.BreakfastProvider,
                sourceUri: new Uri($"pubsub:///{TopicName}"),
                payload: message);

            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ICustomerFeedbackAlertService>();

            service.ProcessFeedbackAsync(
                message.FeedbackId,
                message.CustomerName,
                message.RecipeName,
                message.Rating,
                message.Comments,
                message.ReceivedAt).GetAwaiter().GetResult();

            _logger.LogInformation("In-memory consumer processed {EventType} for feedback {FeedbackId}",
                EventTypeName, message.FeedbackId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process {EventType} message", EventTypeName);
        }
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
