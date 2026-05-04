using Google.Api.Gax;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Hosting;

namespace BreakfastProvider.Tests.Component.Shared.Fakes.PubSub;

/// <summary>
/// Background service that consumes messages from a real Pub/Sub emulator in Docker
/// mode. Creates a unique subscription per topic on startup, pulls messages in a loop,
/// and stores the raw JSON payloads in the shared <see cref="ConsumedPubSubMessageStore"/>
/// for test assertions. Mirrors the <see cref="Kafka.RawJsonKafkaConsumer"/> pattern.
/// </summary>
public class RawJsonPubSubConsumer : BackgroundService
{
    private readonly ConsumedPubSubMessageStore _store;
    private readonly string _eventTypeName;
    private readonly SubscriptionName _subscriptionName;
    private readonly TopicName _topicName;
    private readonly SubscriberServiceApiClient _subscriberClient;

    public RawJsonPubSubConsumer(
        string projectId,
        string topicId,
        string eventTypeName,
        ConsumedPubSubMessageStore store)
    {
        _store = store;
        _eventTypeName = eventTypeName;
        _topicName = TopicName.FromProjectTopic(projectId, topicId);

        // Unique subscription name avoids contention with production consumer subscriptions
        var subscriptionId = $"{topicId}-test-{Guid.NewGuid():N}";
        _subscriptionName = SubscriptionName.FromProjectSubscription(projectId, subscriptionId);

        _subscriberClient = new SubscriberServiceApiClientBuilder
        {
            EmulatorDetection = EmulatorDetection.EmulatorOrProduction
        }.Build();
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        // Create the test subscription on the emulator
        try
        {
            await _subscriberClient.CreateSubscriptionAsync(
                _subscriptionName, _topicName, pushConfig: null, ackDeadlineSeconds: 60, cancellationToken);
            Console.WriteLine($"[RawJsonPubSubConsumer({_eventTypeName})] Created subscription: {_subscriptionName}");
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
        {
            Console.WriteLine($"[RawJsonPubSubConsumer({_eventTypeName})] Subscription already exists: {_subscriptionName}");
        }

        await base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => StartConsumerLoop(stoppingToken), stoppingToken);
    }

    private async Task StartConsumerLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var response = await _subscriberClient.PullAsync(
                    _subscriptionName, maxMessages: 10, cancellationToken);

                if (response.ReceivedMessages.Count == 0)
                {
                    await Task.Delay(500, cancellationToken);
                    continue;
                }

                var ackIds = new List<string>();

                foreach (var received in response.ReceivedMessages)
                {
                    var json = received.Message.Data.ToStringUtf8();
                    Console.WriteLine($"[RawJsonPubSubConsumer({_eventTypeName})] Consumed message: {json[..Math.Min(json.Length, 100)]}");
                    _store.AddRawJson(_eventTypeName, json);
                    ackIds.Add(received.AckId);
                }

                await _subscriberClient.AcknowledgeAsync(_subscriptionName, ackIds, cancellationToken);
                Console.WriteLine($"[RawJsonPubSubConsumer({_eventTypeName})] Acknowledged {ackIds.Count} messages");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
            {
                break;
            }
            catch (Exception ex)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                Console.WriteLine($"[RawJsonPubSubConsumer({_eventTypeName})] Error: {ex.GetType().Name}: {ex.Message}");
                await Task.Delay(1000, cancellationToken);
            }
        }

        Console.WriteLine($"[RawJsonPubSubConsumer({_eventTypeName})] Consumer loop exited gracefully.");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        // Clean up the test subscription
        try
        {
            await _subscriberClient.DeleteSubscriptionAsync(_subscriptionName, cancellationToken);
            Console.WriteLine($"[RawJsonPubSubConsumer({_eventTypeName})] Deleted subscription: {_subscriptionName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RawJsonPubSubConsumer({_eventTypeName})] Warning: Failed to delete subscription: {ex.Message}");
        }
    }
}
