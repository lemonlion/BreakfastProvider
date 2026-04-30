using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Serialization;

namespace BreakfastProvider.Api.Events;

/// <summary>
/// AsyncAPI metadata for a Kafka event, derived by convention from the event type.
/// </summary>
public record KafkaEventAsyncApiMetadata
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

public interface IKafkaEvent
{
    public string GetEventName() => GetType().Name.Replace("Event", "");
    public string GetVersion() => "1";

    /// <summary>
    /// Returns the AsyncAPI metadata for a given event type. All values are derived by convention
    /// from the type name and its [Description] attribute.
    /// </summary>
    static KafkaEventAsyncApiMetadata GetAsyncApiMetadata(Type eventType)
    {
        var baseName = eventType.Name.Replace("Event", "");
        var humanName = Humanize(baseName);
        var description = eventType.GetCustomAttribute<DescriptionAttribute>()?.Description;

        return new KafkaEventAsyncApiMetadata
        {
            ChannelName = eventType.Name,
            ChannelDescription = description ?? $"Channel for {humanName} events.",
            MessageId = $"{CamelCase(baseName)}Message",
            MessageTitle = $"{humanName} Message",
            MessageSummary = description ?? $"Message for {humanName} events.",
            OperationId = baseName,
            OperationSummary = humanName,
            OperationDescription = description ?? $"Message for {humanName} events.",
            SchemaId = CamelCase(eventType.Name),
            SchemaDescription = description ?? $"Content for the {Humanize(eventType.Name)}."
        };
    }

    // --- Schema helpers ---

    /// <summary>
    /// Builds an AsyncAPI JSON schema property descriptor from a CLR property, using [JsonPropertyName]
    /// for the key and [Description] for the description.
    /// </summary>
    static (string jsonName, string? description, string schemaType, string? format, bool isArray) GetPropertySchema(PropertyInfo property)
    {
        var jsonName = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                       ?? CamelCase(property.Name);
        var description = property.GetCustomAttribute<DescriptionAttribute>()?.Description;

        var (propType, isArray) = GetElementType(property.PropertyType);
        var (schemaType, format) = MapClrTypeToSchemaType(propType);

        return (jsonName, description, schemaType, format, isArray);
    }

    // --- String helpers ---

    /// <summary>Inserts spaces before each uppercase letter (except the first), e.g. "RecipeLog" → "Recipe Log".</summary>
    public static string Humanize(string pascalCase)
    {
        if (string.IsNullOrEmpty(pascalCase)) return pascalCase;
        var chars = new List<char> { pascalCase[0] };
        for (var i = 1; i < pascalCase.Length; i++)
        {
            if (char.IsUpper(pascalCase[i]) && !char.IsUpper(pascalCase[i - 1]))
                chars.Add(' ');
            chars.Add(pascalCase[i]);
        }
        return new string(chars.ToArray());
    }

    /// <summary>Lowercases the first character, e.g. "RecipeLogEvent" → "recipeLogEvent".</summary>
    public static string CamelCase(string pascalCase)
        => string.IsNullOrEmpty(pascalCase) ? pascalCase : char.ToLowerInvariant(pascalCase[0]) + pascalCase[1..];

    private static (Type elementType, bool isArray) GetElementType(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            return (type.GetGenericArguments()[0], true);
        if (type.IsArray)
            return (type.GetElementType()!, true);
        return (type, false);
    }

    private static (string schemaType, string? format) MapClrTypeToSchemaType(Type type)
    {
        if (type == typeof(string)) return ("string", null);
        if (type == typeof(Guid)) return ("string", "uuid");
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset)) return ("string", "date-time");
        if (type == typeof(int) || type == typeof(long)) return ("integer", null);
        if (type == typeof(bool)) return ("boolean", null);
        if (type == typeof(double) || type == typeof(float) || type == typeof(decimal)) return ("number", null);
        return ("string", null);
    }
}
