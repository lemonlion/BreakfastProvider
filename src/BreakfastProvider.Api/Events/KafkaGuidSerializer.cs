using System.Text;
using Confluent.Kafka;

namespace BreakfastProvider.Api.Events;

public class KafkaGuidSerializer : ISerializer<Guid>, IDeserializer<Guid>
{
    public byte[] Serialize(Guid data, SerializationContext context)
        => Encoding.UTF8.GetBytes(data.ToString());

    public Guid Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        => Guid.Parse(Encoding.UTF8.GetString(data));
}
