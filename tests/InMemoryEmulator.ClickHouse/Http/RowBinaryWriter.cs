using System.Globalization;
using System.Text;
using InMemoryEmulator.ClickHouse.Core;

namespace InMemoryEmulator.ClickHouse.Http;

/// <summary>
/// Encodes a <see cref="ClickHouseResultSet"/> as <c>RowBinaryWithNamesAndTypes</c>: a varint column
/// count, the names then the types as varint-length UTF-8 strings, then every row's values in
/// column order — <c>String</c> as varint-length UTF-8, integers and floats little-endian,
/// <c>DateTime</c> as UInt32 unix seconds, <c>Nullable(T)</c> as one flag byte (0 = value follows,
/// 1 = null, nothing follows).
/// </summary>
public static class RowBinaryWriter
{
    public static byte[] WriteWithNamesAndTypes(ClickHouseResultSet resultSet)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            WriteVarint(writer, (ulong)resultSet.Columns.Count);
            foreach (var column in resultSet.Columns)
                WriteString(writer, column.Name);
            foreach (var column in resultSet.Columns)
                WriteString(writer, column.TypeString);

            foreach (var row in resultSet.Rows)
            {
                for (var i = 0; i < resultSet.Columns.Count; i++)
                    WriteValue(writer, resultSet.Columns[i].TypeString, row[i]);
            }
        }

        return stream.ToArray();
    }

    public static void WriteVarint(BinaryWriter writer, ulong value)
    {
        while (value >= 0x80)
        {
            writer.Write((byte)(value | 0x80));
            value >>= 7;
        }

        writer.Write((byte)value);
    }

    public static void WriteString(BinaryWriter writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteVarint(writer, (ulong)bytes.Length);
        writer.Write(bytes);
    }

    public static void WriteValue(BinaryWriter writer, string typeString, object? value)
    {
        if (typeString.StartsWith("Nullable(", StringComparison.Ordinal) && typeString.EndsWith(')'))
        {
            if (value is null or DBNull)
            {
                writer.Write((byte)1);
                return;
            }

            writer.Write((byte)0);
            WriteValue(writer, typeString["Nullable(".Length..^1], value);
            return;
        }

        if (value is null or DBNull)
            throw new InvalidOperationException($"Cannot write NULL to a non-Nullable column of type {typeString}.");

        switch (typeString)
        {
            case "String":
                WriteString(writer, Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
                break;
            case "UInt8":
            case "Bool":
                writer.Write(value is bool flag ? (byte)(flag ? 1 : 0) : Convert.ToByte(value, CultureInfo.InvariantCulture));
                break;
            case "Int8":
                writer.Write(Convert.ToSByte(value, CultureInfo.InvariantCulture));
                break;
            case "UInt16":
                writer.Write(Convert.ToUInt16(value, CultureInfo.InvariantCulture));
                break;
            case "Int16":
                writer.Write(Convert.ToInt16(value, CultureInfo.InvariantCulture));
                break;
            case "UInt32":
                writer.Write(Convert.ToUInt32(value, CultureInfo.InvariantCulture));
                break;
            case "Int32":
                writer.Write(Convert.ToInt32(value, CultureInfo.InvariantCulture));
                break;
            case "UInt64":
                writer.Write(Convert.ToUInt64(value, CultureInfo.InvariantCulture));
                break;
            case "Int64":
                writer.Write(Convert.ToInt64(value, CultureInfo.InvariantCulture));
                break;
            case "Float32":
                writer.Write(Convert.ToSingle(value, CultureInfo.InvariantCulture));
                break;
            case "Float64":
                writer.Write(Convert.ToDouble(value, CultureInfo.InvariantCulture));
                break;
            case "DateTime":
                writer.Write(ToUnixSeconds(value));
                break;
            default:
                throw new NotSupportedException($"The in-memory ClickHouse emulator cannot encode values of type '{typeString}' as RowBinary.");
        }
    }

    private static uint ToUnixSeconds(object value)
    {
        var dateTime = value switch
        {
            DateTime dt => dt,
            DateTimeOffset dto => dto.UtcDateTime,
            _ => Convert.ToDateTime(value, CultureInfo.InvariantCulture)
        };

        var utc = dateTime.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
            : dateTime.ToUniversalTime();

        var seconds = new DateTimeOffset(utc).ToUnixTimeSeconds();
        if (seconds < 0 || seconds > uint.MaxValue)
            throw new NotSupportedException($"DateTime value {utc:O} is outside the ClickHouse DateTime range (1970-2106).");

        return (uint)seconds;
    }
}
