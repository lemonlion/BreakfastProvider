using Confluent.Kafka;
using BreakfastProvider.Api.Configuration;
using Microsoft.Extensions.Hosting;

namespace BreakfastProvider.Tests.Component.LightBDD.Fakes.Kafka;

/// <summary>
/// Background service that consumes messages from a real Kafka broker in Docker
/// mode using raw string deserialization, avoiding any reference to concrete event
/// model types. Messages are stored as JSON in the shared
/// <see cref="ConsumedKafkaMessageStore"/> and can be deserialized to any
/// structurally-compatible type at read time.
/// </summary>
public class RawJsonKafkaConsumer : BackgroundService
{
    private readonly ConsumedKafkaMessageStore _consumedKafkaMessageStore;
    private readonly IConsumer<string, string> _consumer;
    private readonly string _eventTypeName;

    public RawJsonKafkaConsumer(KafkaConfig kafkaConfig, string eventTypeName, ConsumedKafkaMessageStore store)
    {
        _consumedKafkaMessageStore = store;
        _eventTypeName = eventTypeName;

        if (!kafkaConfig.ConsumerConfigurations.TryGetValue(eventTypeName, out var topicConfig))
        {
            throw new InvalidOperationException($"No consumer configuration found for event type '{eventTypeName}'.");
        }

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaConfig.BootstrapServers,
            GroupId = $"{topicConfig.TopicName}_componenttest_{Guid.NewGuid()}",
            ClientId = "client.id",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            SecurityProtocol = SecurityProtocol.SaslSsl,
            SaslMechanism = SaslMechanism.Plain,
            SaslUsername = topicConfig.ApiKey,
            SaslPassword = topicConfig.ApiSecret,
            SslEndpointIdentificationAlgorithm = SslEndpointIdentificationAlgorithm.None
        };

        _consumer = new ConsumerBuilder<string, string>(consumerConfig)
            .SetKeyDeserializer(Deserializers.Utf8)
            .SetValueDeserializer(Deserializers.Utf8)
            .Build();

        _consumer.Subscribe(topicConfig.TopicName);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => StartConsumerLoop(stoppingToken), stoppingToken);
    }

    private void StartConsumerLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(1));
                if (consumeResult?.Message is null)
                    continue;

                if (cancellationToken.IsCancellationRequested)
                    break;

                var key = consumeResult.Message.Key ?? string.Empty;
                var json = consumeResult.Message.Value;

                Console.WriteLine($"[RawJsonKafkaConsumer({_eventTypeName})] Consumed message with key: {key}");
                _consumedKafkaMessageStore.AddRawJson(_eventTypeName, key, json);
                Console.WriteLine($"[RawJsonKafkaConsumer({_eventTypeName})] Added message with key: {key} to the consumed message store");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var message = $"[RawJsonKafkaConsumer({_eventTypeName})] Caught exception {ex.GetType().Name} with message {ex.Message}";
                if (ex is ConsumeException consumeException)
                    message += $" and error {consumeException.Error.Code} | {consumeException.Error.Reason}";
                Console.WriteLine(message);
                break;
            }
        }

        Console.WriteLine($"[RawJsonKafkaConsumer({_eventTypeName})] Consumer loop exited gracefully.");
    }

    public override void Dispose()
    {
        try
        {
            _consumer.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RawJsonKafkaConsumer({_eventTypeName})] Error closing consumer: {ex.Message}");
        }

        try
        {
            _consumer.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RawJsonKafkaConsumer({_eventTypeName})] Error disposing consumer: {ex.Message}");
        }

        base.Dispose();
    }
}
