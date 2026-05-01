using System.Diagnostics;
using System.Text.Json;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Api.Telemetry;

namespace BreakfastProvider.Api.Events;

public class PubSubEventPublisher<T> where T : IPubSubEvent
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly PubSubConfig? _config;
    private readonly PublisherClient? _publisher;
    private readonly ILogger<PubSubEventPublisher<T>>? _logger;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public PubSubEventPublisher(
        IOptions<PubSubConfig> config,
        PublisherClient publisher,
        ILogger<PubSubEventPublisher<T>> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _config = config.Value;
        _publisher = publisher;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    protected internal PubSubEventPublisher() { }

    public virtual async Task PublishEvent(T @event, CancellationToken cancellationToken = default)
    {
        if (_publisher is null)
            return;

        var eventName = @event.GetEventName();
        var messageId = Guid.NewGuid().ToString();
        var tenant = _httpContextAccessor.HttpContext?.Request.Headers["Tenant"].FirstOrDefault();
        var topicId = GetTopicId();

        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("PubSubPublish", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "gcp_pubsub");
        activity?.SetTag("messaging.destination.name", topicId);
        activity?.SetTag("messaging.message.id", messageId);

        var stopwatch = Stopwatch.StartNew();
        var isSuccess = false;

        try
        {
            var json = JsonSerializer.Serialize(@event, SerializerOptions);
            var message = new PubsubMessage
            {
                Data = ByteString.CopyFromUtf8(json),
                Attributes =
                {
                    ["ce_specversion"] = "1.0",
                    ["ce_type"] = BuildCeType(eventName, @event.GetVersion()),
                    ["ce_source"] = _config.SourceUrl,
                    ["ce_id"] = messageId,
                    ["ce_time"] = DateTime.UtcNow.ToString("O"),
                    ["ce_traceparent"] = Activity.Current?.Id ?? "",
                    ["ce_tenant"] = tenant ?? "",
                    ["content-type"] = "application/json"
                }
            };

            _logger.LogInformation("Publishing {EventType} to Pub/Sub topic {TopicId} — Id: {MessageId}",
                typeof(T).Name, topicId, messageId);

            var publishedId = await _publisher.PublishAsync(message);

            _logger.LogInformation("Successfully published {EventType} to Pub/Sub — Id: {MessageId}, ServerMessageId: {ServerMessageId}",
                typeof(T).Name, messageId, publishedId);

            isSuccess = true;
            activity?.SetTag("messaging.gcp_pubsub.message_id", publishedId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish {EventType} to Pub/Sub — Id: {MessageId}", typeof(T).Name, messageId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        }
        finally
        {
            stopwatch.Stop();
            DiagnosticsConfig.PubSubPublishDuration.Record(stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("messaging.destination.name", topicId),
                new KeyValuePair<string, object?>("success", isSuccess));
        }
    }

    private string GetTopicId()
        => _config.PublisherConfigurations[typeof(T).Name].TopicId;

    private string BuildCeType(string eventName, string version)
    {
        var reversedDomain = string.Join(".", _config.DomainName.Split('.').Reverse());
        return $"{reversedDomain}.BreakfastProvider.Api.{eventName}.v{version}".ToLower();
    }
}
