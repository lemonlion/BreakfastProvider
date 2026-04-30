using Confluent.Kafka;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Api.Events;
using Microsoft.Extensions.Hosting;

namespace BreakfastProvider.Tests.Component.Shared.Fakes.Kafka;

/// <summary>
/// Background service that consumes messages from a real Kafka broker in Docker
/// mode and writes them to the shared <see cref="ConsumedKafkaMessageStore"/>.
/// In in-memory mode this consumer is NOT started — <see cref="InMemoryProducer{TKey,TValue}"/>
/// writes directly to the store instead.
/// </summary>
public class KafkaConsumer<T> : BackgroundService where T : IKafkaEvent
{
    private readonly ConsumedKafkaMessageStore _consumedKafkaMessageStore;
    private readonly IConsumer<Guid, T> _consumer;

    public KafkaConsumer(KafkaConfig kafkaConfig, ConsumedKafkaMessageStore store)
    {
        _consumedKafkaMessageStore = store;

        if (!kafkaConfig.ConsumerConfigurations.TryGetValue(typeof(T).Name, out var topicConfig))
        {
            throw new InvalidOperationException($"No consumer configuration found for type {typeof(T).Name}.");
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

        _consumer = new ConsumerBuilder<Guid, T>(consumerConfig)
            .SetKeyDeserializer(new KafkaGuidSerializer())
            .SetValueDeserializer(new KafkaJsonSerializer<T>())
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
                var consumeResult = _consumer.Consume(cancellationToken);
                if (consumeResult.Message is null)
                    continue;

                Console.WriteLine($"[KafkaConsumer<{typeof(T).Name}>] Consumed message with Id: {consumeResult.Message.Key}");
                _consumedKafkaMessageStore.Add(consumeResult.Message);
                Console.WriteLine($"[KafkaConsumer<{typeof(T).Name}>] Added message with Id: {consumeResult.Message.Key} to the consumed message store");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                var message = $"[KafkaConsumer<{typeof(T).Name}>] Caught exception {ex.GetType().Name} with message {ex.Message}";
                if (ex is ConsumeException consumeException)
                    message += $" and error {consumeException.Error.Code} | {consumeException.Error.Reason}";
                Console.WriteLine(message);
                break;
            }
        }
    }

    public override void Dispose()
    {
        _consumer.Close();
        _consumer.Dispose();
        base.Dispose();
    }
}
