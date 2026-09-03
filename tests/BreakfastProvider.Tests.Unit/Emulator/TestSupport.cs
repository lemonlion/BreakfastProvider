using System.Net;
using System.Net.Http.Headers;
using ClickHouse.Driver.ADO;
using InMemoryEmulator.ClickHouse.Core;
using InMemoryEmulator.ClickHouse.Http;

namespace BreakfastProvider.Tests.Unit.Emulator;

internal static class TestSupport
{
    public static string DdlPath => Path.Combine(AppContext.BaseDirectory, "Emulator", "001-kitchen-analytics.sql");

    public static string Ddl => File.ReadAllText(DdlPath);

    public static IReadOnlyDictionary<string, string> Params(params (string Name, string Value)[] values) =>
        values.ToDictionary(v => v.Name, v => v.Value, StringComparer.Ordinal);

    public static IReadOnlyDictionary<string, string> NoParams { get; } = new Dictionary<string, string>();

    public static ClickHouseResultSet ResultSet(IEnumerable<(string Name, string Type)> columns, params object?[][] rows) =>
        new(columns.Select(c => new ClickHouseColumn(c.Name, c.Type)).ToList(), rows);
}

/// <summary>
/// Answers the driver's handshake itself and returns a fixed payload for every other request,
/// recording what the driver sent. Used to make the driver the oracle for the encoders.
/// </summary>
internal sealed class StubClickHouseHandler(byte[] payload, string? contentType = null) : HttpMessageHandler
{
    public List<(Uri Uri, string Body, HttpRequestMessage Request)> Requests { get; } = [];

    public static ClickHouseConnection Connect(byte[] payload, out StubClickHouseHandler handler)
    {
        handler = new StubClickHouseHandler(payload);
        return new ClickHouseConnection("Host=stub;Port=8123;Compression=false;Database=stubdb", new HttpClient(handler));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add((request.RequestUri!, body, request));

        if (body.Contains("version()", StringComparison.OrdinalIgnoreCase))
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($"{InMemoryClickHouseHandler.ServerVersion}\t{InMemoryClickHouseHandler.Timezone}\n")
            };
        }

        var content = new ByteArrayContent(payload);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType ?? "application/octet-stream");
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }
}
