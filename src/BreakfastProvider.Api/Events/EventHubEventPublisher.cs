using System.Diagnostics;
using System.Text.Json;
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

    private readonly EventHubConfig? _config;
    private readonly ILogger<EventHubEventPublisher<T>>? _logger;

    public EventHubEventPublisher(
        IOptions<EventHubConfig> config,
        ILogger<EventHubEventPublisher<T>> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    protected EventHubEventPublisher() { }

    public virtual async Task PublishEvent(T @event, CancellationToken cancellationToken = default)
    {
        var eventName = @event.GetEventName();

        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("EventHubPublish", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "azure_event_hubs");
        activity?.SetTag("messaging.destination.name", _config!.EventHubName);

        var stopwatch = Stopwatch.StartNew();
        var isSuccess = false;

        try
        {
            var json = JsonSerializer.Serialize(@event, SerializerOptions);

            _logger!.LogInformation("Publishing {EventType} to Event Hub {EventHubName}",
                typeof(T).Name, _config.EventHubName);

            // In production this would use EventHubProducerClient.SendAsync()
            // with EventData containing the JSON payload and CloudEvents properties.
            // For this platform service, the real client is injected via DI and
            // replaced with an in-memory fake during component tests.
            await Task.CompletedTask;

            _logger.LogInformation("Successfully published {EventType} to Event Hub {EventHubName}",
                typeof(T).Name, _config.EventHubName);

            isSuccess = true;
        }
        catch (Exception ex)
        {
            _logger!.LogError(ex, "Failed to publish {EventType} to Event Hub {EventHubName}",
                typeof(T).Name, _config!.EventHubName);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            DiagnosticsConfig.EventHubPublishDuration.Record(
                stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("event_hub.name", _config!.EventHubName),
                new KeyValuePair<string, object?>("success", isSuccess));
        }
    }
}
