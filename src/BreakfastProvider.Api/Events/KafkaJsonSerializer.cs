using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace BreakfastProvider.Api.Events;

public sealed class KafkaJsonSerializer<T>(IOptions<JsonSerializerOptions>? opts = null) : ISerializer<T>, IDeserializer<T>
{
    private readonly JsonSerializerOptions _opts = opts?.Value ?? new JsonSerializerOptions
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public byte[] Serialize(T data, SerializationContext context) =>
        JsonSerializer.SerializeToUtf8Bytes(data, _opts);

    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context) =>
        JsonSerializer.Deserialize<T>(data, _opts)!;
}
