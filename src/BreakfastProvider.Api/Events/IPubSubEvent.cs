using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Serialization;

namespace BreakfastProvider.Api.Events;

/// <summary>
/// AsyncAPI metadata for a Pub/Sub event, derived by convention from the event type.
/// </summary>
public record PubSubEventAsyncApiMetadata
{
    public required string ChannelName { get; init; }
    public required string ChannelDescription { get; init; }
    public required string MessageId { get; init; }
    public required string MessageTitle { get; init; }
    public required string MessageSummary { get; init; }
    public required string OperationId { get; init; }
    public required string OperationSummary { get; init; }
    public required string OperationDescription { get; init; }
    public required string SchemaId { get; init; }
    public required string SchemaDescription { get; init; }
}

public interface IPubSubEvent
{
    public string GetEventName() => GetType().Name.Replace("Event", "");
    public string GetVersion() => "1";

    /// <summary>
    /// Returns the AsyncAPI metadata for a given event type. All values are derived by convention
    /// from the type name and its [Description] attribute.
    /// </summary>
    static PubSubEventAsyncApiMetadata GetAsyncApiMetadata(Type eventType)
    {
        var baseName = eventType.Name.Replace("Event", "");
        var humanName = IKafkaEvent.Humanize(baseName);
        var description = eventType.GetCustomAttribute<DescriptionAttribute>()?.Description;

        return new PubSubEventAsyncApiMetadata
        {
            ChannelName = eventType.Name,
            ChannelDescription = description ?? $"Channel for {humanName} events.",
            MessageId = $"{IKafkaEvent.CamelCase(baseName)}Message",
            MessageTitle = $"{humanName} Message",
            MessageSummary = description ?? $"Message for {humanName} events.",
            OperationId = $"PubSub{baseName}",
            OperationSummary = humanName,
            OperationDescription = description ?? $"Message for {humanName} events.",
            SchemaId = IKafkaEvent.CamelCase(eventType.Name),
            SchemaDescription = description ?? $"Content for the {IKafkaEvent.Humanize(eventType.Name)}."
        };
    }

    /// <summary>
    /// Builds an AsyncAPI JSON schema property descriptor from a CLR property, using [JsonPropertyName]
    /// for the key and [Description] for the description.
    /// </summary>
    static (string jsonName, string? description, string schemaType, string? format, bool isArray) GetPropertySchema(PropertyInfo property)
        => IKafkaEvent.GetPropertySchema(property);
}
