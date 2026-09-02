using System.Text.Json;
using BreakfastProvider.Api.Configuration;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace BreakfastProvider.Api.Reporting;

/// <summary>
/// Background service that consumes <c>OrderServedEvent</c> messages from Kafka,
/// stores them in ClickHouse, sends a notification, and calls the Kitchen Service.
/// </summary>
public class KafkaOrderServedConsumerService(
    IOptions<KafkaConfig> kafkaOptions,
    IServiceScopeFactory scopeFactory,
    ILogger<KafkaOrderServedConsumerService> logger) : BackgroundService
{
    private const string EventTypeName = "OrderServedEvent";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var kafkaConfig = kafkaOptions.Value;

        if (!kafkaConfig.ConsumerConfigurations.TryGetValue(EventTypeName, out var topicConfig))
        {
            logger.LogWarning("No Kafka consumer configuration found for {EventType}. Order served consumer will not start.", EventTypeName);
            return;
        }

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaConfig.BootstrapServers,
            GroupId = $"{topicConfig.TopicName}_service_time_analysis_{Environment.MachineName}",
            ClientId = "order-served-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        if (!string.IsNullOrEmpty(topicConfig.ApiKey))
        {
            consumerConfig.SecurityProtocol = SecurityProtocol.SaslSsl;
            consumerConfig.SaslMechanism = SaslMechanism.Plain;
            consumerConfig.SaslUsername = topicConfig.ApiKey;
            consumerConfig.SaslPassword = topicConfig.ApiSecret;
            consumerConfig.SslEndpointIdentificationAlgorithm = SslEndpointIdentificationAlgorithm.None;
            consumerConfig.SslCaLocation = kafkaConfig.SslCaLocation;
        }

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig)
            .SetKeyDeserializer(Deserializers.Utf8)
            .SetValueDeserializer(Deserializers.Utf8)
            .Build();

        consumer.Subscribe(topicConfig.TopicName);
        logger.LogInformation("Order served Kafka consumer started on topic {Topic}", topicConfig.TopicName);

        await Task.Run(() => ConsumeLoop(consumer, stoppingToken), stoppingToken);
    }

    private void ConsumeLoop(IConsumer<string, string> consumer, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(TimeSpan.FromSeconds(1));
                if (result?.Message?.Value is null) continue;

                ProcessMessage(result.Message.Value, cancellationToken).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException) { break; }
            catch (ConsumeException ex)
            {
                logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing Kafka message for service time analysis");
            }
        }
    }

    private async Task ProcessMessage(string json, CancellationToken cancellationToken)
    {
        var servedEvent = JsonSerializer.Deserialize<OrderServedMessage>(json, JsonOptions);
        if (servedEvent is null) return;

        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IServiceTimeAnalysisService>();

        await service.ProcessOrderServedAsync(
            servedEvent.ServiceId,
            servedEvent.OrderId,
            servedEvent.ItemType,
            servedEvent.WaitSeconds,
            servedEvent.ServedAt,
            cancellationToken);
    }

    private class OrderServedMessage
    {
        public Guid ServiceId { get; set; }
        public Guid OrderId { get; set; }
        public string ItemType { get; set; } = string.Empty;
        public decimal WaitSeconds { get; set; }
        public DateTime ServedAt { get; set; }
    }
}
