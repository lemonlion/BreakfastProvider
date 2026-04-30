using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BreakfastProvider.Tests.Component.Shared.Util;

public static class MutateRequestExtensions
{
    public static T GetWithPropertyRemoved<T>(this T @object, string propertyName)
    {
        return @object.GetWithPropertyValueChanged(propertyName, null);
    }

    public static T GetWithPropertyValueChanged<T>(this T @object, string propertyPath, object? propertyValue)
    {
        var objectAsJson = JsonSerializer.SerializeToNode(@object);

        var parts = propertyPath.Split('.');
        JsonNode? current = objectAsJson;
        for (int i = 0; i < parts.Length - 1; i++)
            current = NavigateSegment(current!, parts[i]);

        var lastSegment = parts[^1];
        var targetNode = current![lastSegment];
        var typedValue = CoerceToJsonNode(propertyValue, typeof(T), propertyPath);
        targetNode!.ReplaceWith(typedValue);
        return objectAsJson!.Deserialize<T>()!;
    }

    private static JsonNode? CoerceToJsonNode(object? value, Type rootType, string propertyPath)
    {
        if (value is null or "")
            return null;

        if (value is not string stringValue)
            return JsonValue.Create(value);

        var propertyType = ResolvePropertyType(rootType, propertyPath);
        if (propertyType is null)
            return JsonValue.Create(stringValue);

        var underlying = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (underlying == typeof(int) && int.TryParse(stringValue, out var intVal))
            return JsonValue.Create(intVal);
        if (underlying == typeof(long) && long.TryParse(stringValue, out var longVal))
            return JsonValue.Create(longVal);
        if (underlying == typeof(double) && double.TryParse(stringValue, out var doubleVal))
            return JsonValue.Create(doubleVal);
        if (underlying == typeof(decimal) && decimal.TryParse(stringValue, out var decimalVal))
            return JsonValue.Create(decimalVal);
        if (underlying == typeof(bool) && bool.TryParse(stringValue, out var boolVal))
            return JsonValue.Create(boolVal);
        if (underlying == typeof(Guid) && Guid.TryParse(stringValue, out var guidVal))
            return JsonValue.Create(guidVal.ToString());

        return JsonValue.Create(stringValue);
    }

    private static Type? ResolvePropertyType(Type rootType, string propertyPath)
    {
        var currentType = rootType;
        foreach (var segment in propertyPath.Split('.'))
        {
            var propName = segment;
            var bracketIndex = segment.IndexOf('[');
            if (bracketIndex >= 0)
                propName = segment[..bracketIndex];

            var prop = currentType.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop is null) return null;

            currentType = prop.PropertyType;
            if (bracketIndex >= 0)
            {
                // Array/List element type
                var elementType = currentType.IsArray
                    ? currentType.GetElementType()!
                    : currentType.GetGenericArguments().FirstOrDefault() ?? currentType;
                currentType = elementType;
            }
        }
        return currentType;
    }

    private static JsonNode? NavigateSegment(JsonNode current, string segment)
    {
        var bracketIndex = segment.IndexOf('[');
        if (bracketIndex < 0)
            return current[segment];

        var propName = segment[..bracketIndex];
        var indexStr = segment[(bracketIndex + 1)..segment.IndexOf(']')];
        return current[propName]!.AsArray()[int.Parse(indexStr)];
    }
}
