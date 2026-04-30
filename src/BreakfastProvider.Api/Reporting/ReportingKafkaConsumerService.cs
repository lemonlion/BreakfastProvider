using System.Text.Json;
using BreakfastProvider.Api.Configuration;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace BreakfastProvider.Api.Reporting;

/// <summary>
/// Background service that consumes RecipeLogEvent messages from Kafka
/// and feeds them into the reporting database via <see cref="IReportingIngester"/>.
/// </summary>
public class ReportingKafkaConsumerService(
    IOptions<KafkaConfig> kafkaOptions,
    IServiceScopeFactory scopeFactory,
    ILogger<ReportingKafkaConsumerService> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var kafkaConfig = kafkaOptions.Value;

        if (!kafkaConfig.ConsumerConfigurations.TryGetValue("RecipeLogEvent", out var topicConfig))
        {
            logger.LogWarning("No Kafka consumer configuration found for RecipeLogEvent. Reporting consumer will not start.");
            return;
        }

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaConfig.BootstrapServers,
            GroupId = $"{topicConfig.TopicName}_reporting_{Environment.MachineName}",
            ClientId = "reporting-consumer",
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
        }

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig)
            .SetKeyDeserializer(Deserializers.Utf8)
            .SetValueDeserializer(Deserializers.Utf8)
            .Build();

        consumer.Subscribe(topicConfig.TopicName);
        logger.LogInformation("Reporting Kafka consumer started on topic {Topic}", topicConfig.TopicName);

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
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing Kafka message for reporting");
            }
        }
    }

    private async Task ProcessMessage(string json, CancellationToken cancellationToken)
    {
        var recipeLog = JsonSerializer.Deserialize<RecipeLogMessage>(json, JsonOptions);
        if (recipeLog is null) return;

        using var scope = scopeFactory.CreateScope();
        var ingester = scope.ServiceProvider.GetRequiredService<IReportingIngester>();

        await ingester.IngestRecipeLogAsync(
            recipeLog.OrderId,
            recipeLog.RecipeType,
            recipeLog.Ingredients,
            recipeLog.Toppings,
            recipeLog.LoggedAt,
            cancellationToken);
    }

    private class RecipeLogMessage
    {
        public Guid OrderId { get; set; }
        public string RecipeType { get; set; } = string.Empty;
        public List<string> Ingredients { get; set; } = [];
        public List<string> Toppings { get; set; } = [];
        public DateTime LoggedAt { get; set; }
    }
}
