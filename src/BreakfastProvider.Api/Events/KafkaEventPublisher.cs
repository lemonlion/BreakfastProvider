using System.Diagnostics;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Api.Telemetry;

namespace BreakfastProvider.Api.Events;

public class KafkaEventPublisher<T> : IDisposable where T : IKafkaEvent
{
    private const string JsonMimeTypeAndUtf8Charset = "application/json;charset=utf-8";
    private const string ServiceNamespace = "BreakfastProvider";
    private const string ServiceName = "Api";

    private static readonly JsonSerializerOptions HeaderSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly KafkaConfig _kafkaOpts;
    private readonly ILogger<KafkaEventPublisher<T>> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly (string contentType, ISerializer<T> serializer) _contentTypeAndSerializer;
    private readonly IProducer<Guid, T> _kafkaProducer;

    public KafkaEventPublisher(
        IOptions<KafkaConfig> kafkaOpts,
        IProducerFactory producerFactory,
        ILogger<KafkaEventPublisher<T>> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _kafkaOpts = kafkaOpts.Value;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _contentTypeAndSerializer = GetContentTypeAndSerializer();
        _kafkaProducer = producerFactory.Create(_contentTypeAndSerializer.serializer);
    }

    public async Task PublishEvent(T @event, CancellationToken cancellationToken = default)
    {
        var httpContextHeaders = _httpContextAccessor.HttpContext?.Request.Headers ?? new HeaderDictionary();
        var tenant = httpContextHeaders["Tenant"].FirstOrDefault();

        var message = CreateMessage(@event, @event.GetEventName(), @event.GetVersion(),
            _contentTypeAndSerializer.contentType, tenant);

        var properties = new Dictionary<string, string>
        {
            { KafkaConstants.MessageId, message.Key.ToString() }
        };

        foreach (var property in GetMessageProperties(@event))
        {
            properties.Add(property.Key, property.Value);
        }
        
        var isSuccess = false;
        var resultCode = "";
        var topic = GetTopic(_kafkaOpts);

        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("KafkaPublish", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "kafka");
        activity?.SetTag("messaging.destination.name", topic);
        activity?.SetTag("messaging.message.id", message.Key.ToString());

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromMilliseconds(_kafkaOpts.MessageTimeoutInMilliseconds));

            _logger.LogInformation("Producing {EventType} - Id: {MessageId} | Properties: {@Properties}", typeof(T).Name, message.Key, properties);
            var result = await _kafkaProducer.ProduceAsync(topic, message, cts.Token);
            _logger.LogInformation("Successfully produced {EventType} - Id: {MessageId}{StatusDetail} | Properties: {@Properties}",
                typeof(T).Name, message.Key,
                result.Status != PersistenceStatus.Persisted
                    ? $", but the delivery result returned a status of '{result.Status}' when sent to topic '{topic}'"
                    : "",
                properties);

            isSuccess = result.Status is PersistenceStatus.Persisted or PersistenceStatus.PossiblyPersisted;
            activity?.SetTag("messaging.kafka.persistence_status", result.Status.ToString());
        }
        catch (Exception ex)
        {
            isSuccess = false;

            _logger.LogError("Failed to produce {EventType} - Id: {MessageId} | Properties: {@Properties}", typeof(T).Name, message.Key, properties);
            _logger.LogError(ex, "Exception producing {EventType} - Id: {MessageId} | Properties: {@Properties}", typeof(T).Name, message.Key, properties);

            if (ex is ProduceException<Guid, T> produceException)
            {
                _logger.LogError("Kafka error: {ErrorCode} | {ErrorReason} | Properties: {@Properties}", produceException.Error.Code, produceException.Error.Reason, properties);
                resultCode = produceException.Error.Code.ToString();
            }

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        }
        finally
        {
            stopwatch.Stop();
            var durationMs = stopwatch.Elapsed.TotalMilliseconds;

            DiagnosticsConfig.KafkaPublishDuration.Record(durationMs,
                new KeyValuePair<string, object?>("messaging.destination.name", topic),
                new KeyValuePair<string, object?>("success", isSuccess));

            _logger.LogInformation(
                "Kafka dependency: Server={Server}, Topic={Topic}, Duration={Duration}ms, ResultCode={ResultCode}, Success={Success}",
                _kafkaOpts.BootstrapServers, topic, durationMs, resultCode, isSuccess);
        }

    }

    public void Dispose()
    {
        _kafkaProducer.Flush(_kafkaOpts.FlushTimeout);
        _kafkaProducer.Dispose();
    }
    
    private static string GetTopic(KafkaConfig kafkaSettings) 
        => kafkaSettings.ProducerConfigurations[typeof(T).Name].TopicName;
    
    private (string contentType, ISerializer<T> serializer) GetContentTypeAndSerializer()
        => (JsonMimeTypeAndUtf8Charset, new KafkaJsonSerializer<T>());

    private Message<Guid, T> CreateMessage(T @event, string eventName, string version, string contentType, string? tenant)
    {
        var timestamp = DateTime.UtcNow;

        return new Message<Guid, T>
        {
            Timestamp = new Timestamp(timestamp),
            Headers = SetMessageHeaders(eventName, version, contentType, timestamp, tenant),
            Key = Guid.NewGuid(),
            Value = @event
        };
    }

    private Headers SetMessageHeaders(string eventName, string version, string contentType, DateTime timestamp, string? tenant)
    {
        var correlationId = Activity.Current?.Id;

        var reversedDomain = string.Join(".", _kafkaOpts.DomainName.Split('.').Reverse());
        var ceType = $"{reversedDomain}.{ServiceNamespace}.{ServiceName}.{eventName}.v{version}".ToLower();

        var messageHeaders = new MessageHeaders
        {
            CeSpecVersion = "1.0",
            CeType = ceType,
            CeSource = _kafkaOpts.SourceUrl,
            CeId = Guid.NewGuid(),
            CeTime = timestamp,
            ContentType = contentType,
            CeTraceParent = correlationId ?? "",
            CeTenant = tenant ?? ""
        };

        var headers = new Headers();
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(messageHeaders, HeaderSerializerOptions));

        foreach (var property in document.RootElement.EnumerateObject())
            headers.SetUtf8(property.Name, property.Value.ToString());

        return headers;
    }

    private static Dictionary<string, string> GetMessageProperties(T @event)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(@event));
        return document.RootElement.EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.ToString());
    }
}
