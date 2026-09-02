using System.Numerics;

namespace InMemoryEmulator.ClickHouse.Core;

/// <summary>
/// Type mappings in both directions: ClickHouse DDL types to DuckDB DDL types, and CLR result
/// types back to ClickHouse type strings.
/// </summary>
public static class ClickHouseTypeMap
{
    private static readonly Dictionary<string, string> DdlTypes = new(StringComparer.Ordinal)
    {
        ["String"] = "VARCHAR",
        ["Float64"] = "DOUBLE",
        ["Float32"] = "FLOAT",
        ["DateTime"] = "TIMESTAMP",
        ["Bool"] = "BOOLEAN",
        ["UInt8"] = "UTINYINT",
        ["UInt16"] = "USMALLINT",
        ["UInt32"] = "UINTEGER",
        ["UInt64"] = "UBIGINT",
        ["Int8"] = "TINYINT",
        ["Int16"] = "SMALLINT",
        ["Int32"] = "INTEGER",
        ["Int64"] = "BIGINT",
    };

    /// <summary>Maps a ClickHouse DDL column type to its DuckDB equivalent. <c>Nullable(T)</c> maps to <c>T</c>.</summary>
    public static string DdlTypeToDuckDb(string clickHouseType)
    {
        var type = clickHouseType.Trim();

        if (type.StartsWith("Nullable(", StringComparison.Ordinal) && type.EndsWith(')'))
            return DdlTypeToDuckDb(type["Nullable(".Length..^1]);

        if (DdlTypes.TryGetValue(type, out var duckDbType))
            return duckDbType;

        throw new NotSupportedException(
            $"The in-memory ClickHouse emulator does not support the column type '{clickHouseType}'. " +
            $"Supported DDL types: {string.Join(", ", DdlTypes.Keys)} and Nullable(T) of those.");
    }

    /// <summary>Maps the CLR type of a result column back to a ClickHouse type string.</summary>
    public static string ClrTypeToClickHouse(Type clrType)
    {
        if (clrType == typeof(string)) return "String";
        if (clrType == typeof(double)) return "Float64";
        if (clrType == typeof(float)) return "Float32";
        if (clrType == typeof(DateTime)) return "DateTime";
        if (clrType == typeof(ulong)) return "UInt64";
        if (clrType == typeof(long)) return "Int64";
        if (clrType == typeof(uint)) return "UInt32";
        if (clrType == typeof(int)) return "Int32";
        if (clrType == typeof(ushort)) return "UInt16";
        if (clrType == typeof(short)) return "Int16";
        if (clrType == typeof(byte)) return "UInt8";
        if (clrType == typeof(sbyte)) return "Int8";
        if (clrType == typeof(bool)) return "UInt8";

        if (clrType == typeof(BigInteger))
            throw new NotSupportedException(
                "A result column is a 128-bit integer (DuckDB HUGEINT), which has no cheap RowBinary mapping. " +
                "This is what sum() over an integer column produces — keep numeric columns Float64 and never sum() an integer column.");

        if (clrType == typeof(DateTimeOffset))
            throw new NotSupportedException(
                "A result column is a timestamp with time zone (DuckDB TIMESTAMPTZ), which the emulator does not map. " +
                "Do not use now(); pass DateTime parameters from the application instead.");

        throw new NotSupportedException($"The in-memory ClickHouse emulator cannot map result values of CLR type {clrType} to a ClickHouse type.");
    }
}
