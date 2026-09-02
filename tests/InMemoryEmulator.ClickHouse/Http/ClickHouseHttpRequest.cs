using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace InMemoryEmulator.ClickHouse.Http;

/// <summary>
/// The parts of a ClickHouse HTTP request the emulator cares about: the SQL text (from the body,
/// else the <c>query</c> query-string entry), the output format (a trailing <c>FORMAT x</c>
/// clause, else <c>default_format</c>, else RowBinaryWithNamesAndTypes) and the
/// <c>param_&lt;name&gt;</c> values. <c>database</c>, <c>query_id</c>, <c>session_id</c> and the
/// compression flags are ignored.
/// </summary>
public sealed partial record ClickHouseHttpRequest(
    string Sql,
    string Format,
    IReadOnlyDictionary<string, string> Parameters,
    string? Database)
{
    public const string DefaultFormat = "RowBinaryWithNamesAndTypes";

    [GeneratedRegex(@"\s+FORMAT\s+(?<format>[A-Za-z0-9_]+)\s*;?\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex TrailingFormatRegex();

    public static async Task<ClickHouseHttpRequest> ParseAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var query = ParseQueryString(request.RequestUri?.Query);
        var body = await ReadBodyAsync(request, cancellationToken);

        var sql = body.Length > 0 ? body : query.GetValueOrDefault("query") ?? string.Empty;
        sql = sql.Trim();

        string? format = null;
        var formatMatch = TrailingFormatRegex().Match(sql);
        if (formatMatch.Success)
        {
            format = formatMatch.Groups["format"].Value;
            sql = sql[..formatMatch.Index].TrimEnd();
        }

        format ??= query.GetValueOrDefault("default_format") ?? DefaultFormat;

        var parameters = query
            .Where(kv => kv.Key.StartsWith("param_", StringComparison.Ordinal))
            .ToDictionary(kv => kv.Key["param_".Length..], kv => kv.Value, StringComparer.Ordinal);

        return new ClickHouseHttpRequest(sql, format, parameters, query.GetValueOrDefault("database"));
    }

    private static async Task<string> ReadBodyAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content is null)
            return string.Empty;

        await using var raw = await request.Content.ReadAsStreamAsync(cancellationToken);
        Stream stream = raw;

        var isGzip = request.Content.Headers.ContentEncoding.Any(e => string.Equals(e, "gzip", StringComparison.OrdinalIgnoreCase));
        if (isGzip)
            stream = new GZipStream(raw, CompressionMode.Decompress);

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static Dictionary<string, string> ParseQueryString(string? query)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (string.IsNullOrEmpty(query))
            return result;

        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=');
            var key = Decode(separator < 0 ? pair : pair[..separator]);
            var value = separator < 0 ? string.Empty : Decode(pair[(separator + 1)..]);
            result[key] = value;
        }

        return result;

        // application/x-www-form-urlencoded semantics: '+' is a space, %XX escapes are decoded.
        static string Decode(string text) => Uri.UnescapeDataString(text.Replace('+', ' '));
    }
}
