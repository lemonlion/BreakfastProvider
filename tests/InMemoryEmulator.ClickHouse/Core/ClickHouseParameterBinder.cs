using System.Globalization;
using System.Text.RegularExpressions;

namespace InMemoryEmulator.ClickHouse.Core;

/// <summary>A parameter after binding: the DuckDB parameter name (without the <c>$</c>) and the coerced CLR value.</summary>
public readonly record struct BoundParameter(string Name, object Value);

/// <summary>
/// Rewrites ClickHouse <c>{name:Type}</c> placeholders to DuckDB <c>$name</c> parameters and coerces
/// the raw text values that arrive on the wire (<c>param_&lt;name&gt;=&lt;text&gt;</c>) to CLR values
/// according to the declared placeholder type.
/// </summary>
public static partial class ClickHouseParameterBinder
{
    [GeneratedRegex(@"\{(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*:\s*(?<type>[A-Za-z0-9_]+(?:\([^)]*\))?)\}")]
    private static partial Regex PlaceholderRegex();

    private static readonly string[] DateTimeFormats =
    [
        "yyyy-MM-dd'T'HH:mm:ss",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF",
        "yyyy-MM-dd HH:mm:ss.FFFFFFF",
        "yyyy-MM-dd'T'HH:mm:ss'Z'",
        "yyyy-MM-dd"
    ];

    public static (string Sql, IReadOnlyList<BoundParameter> Parameters) Bind(string sql, IReadOnlyDictionary<string, string> rawValues)
    {
        var bound = new List<BoundParameter>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        var rewritten = PlaceholderRegex().Replace(sql, match =>
        {
            var name = match.Groups["name"].Value;
            var type = match.Groups["type"].Value;

            if (seen.Add(name))
            {
                if (!rawValues.TryGetValue(name, out var raw))
                    throw ClickHouseEmulatorException.UnknownQueryParameter($"Substitution `{name}` is not set");

                bound.Add(new BoundParameter(name, Coerce(raw, type)));
            }

            return "$" + name;
        });

        return (rewritten, bound);
    }

    /// <summary>Converts the wire text of a parameter to the CLR value implied by its ClickHouse type.</summary>
    public static object Coerce(string raw, string clickHouseType)
    {
        var type = clickHouseType.Trim();
        if (type.StartsWith("Nullable(", StringComparison.Ordinal) && type.EndsWith(')'))
            type = type["Nullable(".Length..^1];

        return type switch
        {
            "String" => raw,
            "Float64" => double.Parse(raw, NumberStyles.Float, CultureInfo.InvariantCulture),
            "Float32" => float.Parse(raw, NumberStyles.Float, CultureInfo.InvariantCulture),
            "Int8" => sbyte.Parse(raw, CultureInfo.InvariantCulture),
            "Int16" => short.Parse(raw, CultureInfo.InvariantCulture),
            "Int32" => int.Parse(raw, CultureInfo.InvariantCulture),
            "Int64" => long.Parse(raw, CultureInfo.InvariantCulture),
            "UInt8" => byte.Parse(raw, CultureInfo.InvariantCulture),
            "UInt16" => ushort.Parse(raw, CultureInfo.InvariantCulture),
            "UInt32" => uint.Parse(raw, CultureInfo.InvariantCulture),
            "UInt64" => ulong.Parse(raw, CultureInfo.InvariantCulture),
            "Bool" => raw is "1" or "true" or "True",
            "DateTime" => ParseDateTime(raw),
            _ => throw new NotSupportedException($"The in-memory ClickHouse emulator does not support parameters of type '{clickHouseType}'.")
        };
    }

    private static DateTime ParseDateTime(string raw)
    {
        // The driver formats DateTime values as yyyy-MM-ddTHH:mm:ss with no zone and no conversion;
        // the server interprets them in its own timezone (UTC for the emulator).
        if (DateTime.TryParseExact(raw, DateTimeFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);

        if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unixSeconds))
            return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;

        throw ClickHouseEmulatorException.SyntaxError($"Cannot parse DateTime from '{raw}'");
    }
}
