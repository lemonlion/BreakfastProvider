using Confluent.Kafka;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

namespace BreakfastProvider.Tests.Component.Shared.Fakes.Kafka;

public class InMemoryProducer<TKey, TValue> : NotImplementedClient, IProducer<TKey, TValue>
{
    private readonly ConsumedKafkaMessageStore _consumedKafkaMessageStore;

    public InMemoryProducer(ConsumedKafkaMessageStore consumedKafkaMessageStore)
    {
        _consumedKafkaMessageStore = consumedKafkaMessageStore;
    }

    public void Produce(string topic, Message<TKey, TValue> message, Action<DeliveryReport<TKey, TValue>> deliveryHandler = null)
    {
        ProduceInternal(message);
    }

    public Task<DeliveryResult<TKey, TValue>> ProduceAsync(string topic, Message<TKey, TValue> message, CancellationToken cancellationToken = new())
    {
        ProduceInternal(message);
        
        return Task.FromResult(CreateDeliveryResult(message, topic));
    }

    public Task<DeliveryResult<TKey, TValue>> ProduceAsync(TopicPartition topicPartition, Message<TKey, TValue> message,
        CancellationToken cancellationToken = new())
    {
        ProduceInternal(message);

        return Task.FromResult(CreateDeliveryResult(message, topicPartition.Topic));
    }

    public void Produce(TopicPartition topicPartition, Message<TKey, TValue> message, Action<DeliveryReport<TKey, TValue>> deliveryHandler = null)
    {
        ProduceInternal(message);
    }

    public int Flush(TimeSpan timeout) => 0;

    public void Flush(CancellationToken cancellationToken = new()) { }

    private void ProduceInternal(Message<TKey, TValue> message) => _consumedKafkaMessageStore.Add(message);
    private DeliveryResult<TKey, TValue> CreateDeliveryResult(Message<TKey, TValue> message, string topic) => new()
    {
        Message = message,
        Timestamp = new Timestamp(DateTime.UtcNow, TimestampType.CreateTime),
        Status = PersistenceStatus.Persisted,
        Topic = topic
    };

    #region Not Implemented Methods

    public int Poll(TimeSpan timeout) => throw new NotImplementedException();

    public void InitTransactions(TimeSpan timeout) => throw new NotImplementedException();

    public void BeginTransaction() => throw new NotImplementedException();

    public void CommitTransaction(TimeSpan timeout) => throw new NotImplementedException();

    public void CommitTransaction() => throw new NotImplementedException();

    public void AbortTransaction(TimeSpan timeout) => throw new NotImplementedException();

    public void AbortTransaction() => throw new NotImplementedException();

    public void SendOffsetsToTransaction(IEnumerable<TopicPartitionOffset> offsets, IConsumerGroupMetadata groupMetadata, TimeSpan timeout) =>
        throw new NotImplementedException();

    #endregion
}
