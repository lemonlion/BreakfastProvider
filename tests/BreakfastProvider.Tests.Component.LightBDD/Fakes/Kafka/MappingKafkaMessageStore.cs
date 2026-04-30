namespace BreakfastProvider.Tests.Component.LightBDD.Fakes.Kafka;

public interface IKafkaMessageStore
{
    IReadOnlyList<(string Key, T Message)> GetMessages<T>() where T : class;
}

/// <summary>
/// Adapts the <see cref="ConsumedKafkaMessageStore"/> to the <see cref="IKafkaMessageStore"/>
/// interface, filtering by the source event type name configured at construction time.
/// Callers request any structurally-compatible type <typeparamref name="T"/> at read time;
/// the store deserialises from JSON on demand.
/// </summary>
public class KafkaMessageStore(ConsumedKafkaMessageStore store, string sourceEventTypeName) : IKafkaMessageStore
{
    public IReadOnlyList<(string Key, T Message)> GetMessages<T>() where T : class
        => store.GetMessages<T>(sourceEventTypeName);
}
