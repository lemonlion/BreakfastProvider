using System.Text.Json;
using System.Text.RegularExpressions;

namespace BreakfastProvider.Tests.Component.Shared.Util;

/// <summary>
/// Reads the two forms the GraphQL contract is published in: the introspection response (/graphql/schema.json,
/// <c>{"data":{"__schema":…}}</c>) and the schema definition (/graphql/schema.graphql).
/// </summary>
public static class GraphQLSchemaDocuments
{
    public static string? QueryTypeName(JsonDocument introspection) =>
        Schema(introspection).GetProperty("queryType").GetProperty("name").GetString();

    public static string? TypeDescription(JsonDocument introspection, string typeName) =>
        Type(introspection, typeName) is { } type ? StringOrNull(type, "description") : null;

    public static IReadOnlyList<string> FieldNames(JsonDocument introspection, string typeName) =>
        Fields(introspection, typeName).Select(f => f.GetProperty("name").GetString()!).ToList();

    /// <summary>The type's fields that carry no description.</summary>
    public static IReadOnlyList<string> UndocumentedFields(JsonDocument introspection, string typeName) =>
        Fields(introspection, typeName)
            .Where(f => string.IsNullOrWhiteSpace(StringOrNull(f, "description")))
            .Select(f => f.GetProperty("name").GetString()!)
            .ToList();

    /// <summary>Whether the schema definition's <c>type</c> declares the field.</summary>
    public static bool DeclaresField(string schemaDefinition, string typeName, string fieldName)
    {
        var type = Regex.Match(schemaDefinition, $@"^type {Regex.Escape(typeName)}\b[^{{]*\{{(?<body>.*?)^\}}",
            RegexOptions.Multiline | RegexOptions.Singleline);
        return type.Success
            && Regex.IsMatch(type.Groups["body"].Value, $@"^\s+{Regex.Escape(fieldName)}\s*[(:]", RegexOptions.Multiline);
    }

    private static JsonElement Schema(JsonDocument introspection) =>
        introspection.RootElement.GetProperty("data").GetProperty("__schema");

    private static JsonElement? Type(JsonDocument introspection, string typeName) =>
        Schema(introspection).GetProperty("types").EnumerateArray()
            .Cast<JsonElement?>()
            .FirstOrDefault(t => t!.Value.GetProperty("name").GetString() == typeName);

    private static IEnumerable<JsonElement> Fields(JsonDocument introspection, string typeName) =>
        Type(introspection, typeName) is { } type && type.GetProperty("fields").ValueKind == JsonValueKind.Array
            ? type.GetProperty("fields").EnumerateArray()
            : [];

    private static string? StringOrNull(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
