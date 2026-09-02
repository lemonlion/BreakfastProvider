using System.Globalization;
using System.Text;
using InMemoryEmulator.ClickHouse.Core;

namespace InMemoryEmulator.ClickHouse.Http;

/// <summary>
/// Encodes a <see cref="ClickHouseResultSet"/> as ClickHouse <c>TabSeparated</c> (TSV): one row per
/// line, tab-separated, <c>\n</c> line endings, NULL as <c>\N</c>, tabs/newlines/backslashes escaped.
/// Used for the driver's <c>SELECT version(), timezone() FORMAT TSV</c> handshake.
/// </summary>
public static class TsvWriter
{
    public static string Write(ClickHouseResultSet resultSet, bool withNamesAndTypes = false)
    {
        var builder = new StringBuilder();

        if (withNamesAndTypes)
        {
            builder.AppendJoin('\t', resultSet.Columns.Select(c => Escape(c.Name))).Append('\n');
            builder.AppendJoin('\t', resultSet.Columns.Select(c => Escape(c.TypeString))).Append('\n');
        }

        foreach (var row in resultSet.Rows)
        {
            builder.AppendJoin('\t', row.Select(Format)).Append('\n');
        }

        return builder.ToString();
    }

    public static string Format(object? value) => value switch
    {
        null or DBNull => "\\N",
        bool flag => flag ? "1" : "0",
        DateTime dateTime => dateTime.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        double d => d.ToString("R", CultureInfo.InvariantCulture),
        float f => f.ToString("R", CultureInfo.InvariantCulture),
        string s => Escape(s),
        _ => Escape(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty)
    };

    private static string Escape(string text) => text
        .Replace("\\", "\\\\")
        .Replace("\t", "\\t")
        .Replace("\n", "\\n")
        .Replace("\r", "\\r");
}
