using System.Diagnostics;
using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Options;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Api.Telemetry;

namespace BreakfastProvider.Api.Events;

public class EventHubEventPublisher<T> where T : IEventHubEvent
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly EventHubConfig _config;
    private readonly EventHubProducerClient _producerClient;
    private readonly ILogger<EventHubEventPublisher<T>> _logger;

    public EventHubEventPublisher(
        IOptions<EventHubConfig> config,
        EventHubProducerClient producerClient,
        ILogger<EventHubEventPublisher<T>> logger)
    {
        _config = config.Value;
        _producerClient = producerClient;
        _logger = logger;
    }

    protected internal EventHubEventPublisher()
    {
        _config = null!;
        _producerClient = null!;
        _logger = null!;
    }

    public virtual async Task PublishEvent(T @event, CancellationToken cancellationToken = default)
    {
        if (_producerClient is null)
            throw new InvalidOperationException(
                $"EventHubEventPublisher<{typeof(T).Name}> was created without a producer client. " +
                "Ensure EventHubConfig.ConnectionString is configured.");

        var eventName = @event.GetEventName();

        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("EventHubPublish", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "azure_event_hubs");
        activity?.SetTag("messaging.destination.name", _config.EventHubName);

        var stopwatch = Stopwatch.StartNew();
        var isSuccess = false;

        try
        {
            var json = JsonSerializer.Serialize(@event, SerializerOptions);

            _logger.LogInformation("Publishing {EventType} to Event Hub {EventHubName}",
                typeof(T).Name, _config.EventHubName);

            var eventData = new EventData(json)
            {
                ContentType = "application/json"
            };
            eventData.Properties["ce_type"] = typeof(T).Name;
            eventData.Properties["ce_source"] = "BreakfastProvider.Api";
            eventData.Properties["ce_id"] = Guid.NewGuid().ToString();
            eventData.Properties["ce_time"] = DateTime.UtcNow.ToString("O");

            await _producerClient.SendAsync([eventData], cancellationToken);

            _logger.LogInformation("Successfully published {EventType} to Event Hub {EventHubName}",
                typeof(T).Name, _config.EventHubName);

            isSuccess = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish {EventType} to Event Hub {EventHubName}",
                typeof(T).Name, _config.EventHubName);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            DiagnosticsConfig.EventHubPublishDuration.Record(
                stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("event_hub.name", _config.EventHubName),
                new KeyValuePair<string, object?>("success", isSuccess));
        }
    }
}
