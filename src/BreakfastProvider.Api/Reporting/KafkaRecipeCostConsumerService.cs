using System.Text.Json;
using BreakfastProvider.Api.Configuration;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace BreakfastProvider.Api.Reporting;

/// <summary>
/// Background service that consumes <c>RecipeCostCalculatedEvent</c> messages from Kafka,
/// stores them in BigQuery, sends a notification, and calls the Kitchen Service.
/// </summary>
public class KafkaRecipeCostConsumerService(
    IOptions<KafkaConfig> kafkaOptions,
    IServiceScopeFactory scopeFactory,
    ILogger<KafkaRecipeCostConsumerService> logger) : BackgroundService
{
    private const string EventTypeName = "RecipeCostCalculatedEvent";

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
            logger.LogWarning("No Kafka consumer configuration found for {EventType}. Recipe cost consumer will not start.", EventTypeName);
            return;
        }

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaConfig.BootstrapServers,
            GroupId = $"{topicConfig.TopicName}_cost_analysis_{Environment.MachineName}",
            ClientId = "recipe-cost-consumer",
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
        logger.LogInformation("Recipe cost Kafka consumer started on topic {Topic}", topicConfig.TopicName);

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
                logger.LogError(ex, "Error processing Kafka message for recipe cost analysis");
            }
        }
    }

    private async Task ProcessMessage(string json, CancellationToken cancellationToken)
    {
        var costEvent = JsonSerializer.Deserialize<RecipeCostMessage>(json, JsonOptions);
        if (costEvent is null) return;

        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IRecipeCostAnalysisService>();

        await service.ProcessCostCalculationAsync(
            costEvent.CalculationId,
            costEvent.RecipeName,
            costEvent.Ingredients,
            costEvent.TotalCost,
            costEvent.Currency,
            costEvent.CalculatedAt,
            cancellationToken);
    }

    private class RecipeCostMessage
    {
        public Guid CalculationId { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public List<string> Ingredients { get; set; } = [];
        public decimal TotalCost { get; set; }
        public string Currency { get; set; } = "GBP";
        public DateTime CalculatedAt { get; set; }
    }
}
