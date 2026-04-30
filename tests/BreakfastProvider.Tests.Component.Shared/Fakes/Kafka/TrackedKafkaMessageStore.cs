using System.Collections.Concurrent;
using TestTrackingDiagrams.Tracking;

namespace BreakfastProvider.Tests.Component.Shared.Fakes.Kafka;

/// <summary>
/// Decorator around <see cref="IKafkaMessageStore"/> that logs a Kafka consume
/// event to <see cref="MessageTracker"/> when the test reads messages from the
/// store, so that the consume side of the Kafka flow appears in PlantUML
/// sequence diagrams.
///
/// In in-memory mode there is no real Kafka consumer — the
/// <see cref="InMemoryProducer{TKey,TValue}"/> writes directly to
/// <see cref="ConsumedKafkaMessageStore"/>. This decorator makes the
/// test-side read visible as a "Consume (Kafka)" interaction.
///
/// Tracks one consume arrow per test to avoid duplicate arrows from retry
/// loops and messages produced by other parallel tests in the shared store.
/// </summary>
public class TrackedKafkaMessageStore(
    IKafkaMessageStore inner,
    MessageTracker tracker,
    Func<(string Name, string Id)> testInfoFetcher,
    string topicName,
    string consumerName) : IKafkaMessageStore
{
    private readonly ConcurrentDictionary<string, bool> _trackedTests = new();

    public IReadOnlyList<(string Key, T Message)> GetMessages<T>() where T : class
    {
        var messages = inner.GetMessages<T>();

        if (messages.Count > 0)
        {
            // Track one consume arrow per test (not per message or per poll)
            var testId = testInfoFetcher().Id;
            if (_trackedTests.TryAdd(testId, true))
            {
                tracker.TrackSendEvent(
                    protocol: "Consume (Kafka)",
                    destinationName: consumerName,
                    destinationUri: new Uri($"kafka:///{topicName}"),
                    payload: messages[0].Message);
            }
        }

        return messages;
    }
}
