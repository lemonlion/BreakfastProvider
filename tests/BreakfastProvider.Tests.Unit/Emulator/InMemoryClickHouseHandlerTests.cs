using System.IO.Compression;
using System.Net;
using System.Text;
using InMemoryEmulator.ClickHouse.Core;
using InMemoryEmulator.ClickHouse.Http;
using static BreakfastProvider.Tests.Unit.Emulator.TestSupport;

namespace BreakfastProvider.Tests.Unit.Emulator;

/// <summary>Raw HTTP against the handler — the contract the driver relies on, without the driver.</summary>
public sealed class InMemoryClickHouseHandlerTests : IDisposable
{
    private const string BaseUrl = "http://inmemory:8123/?enable_http_compression=false&default_format=RowBinaryWithNamesAndTypes&database=kitchen_analytics";

    private readonly DuckDbClickHouseQueryEngine _engine = new("kitchen_analytics");
    private readonly HttpClient _client;

    public InMemoryClickHouseHandlerTests()
    {
        _engine.ExecuteDdl(Ddl);
        _client = new HttpClient(new InMemoryClickHouseHandler(_engine));
    }

    public void Dispose()
    {
        _client.Dispose();
        _engine.Dispose();
    }

    [Fact]
    public async Task Handshake_returns_the_docker_images_version_and_utc_as_tsv()
    {
        var response = await Post("SELECT version(), timezone() FORMAT TSV", "text/plain");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("25.8.33.6\tUTC\n");
        response.Headers.GetValues("X-ClickHouse-Timezone").Should().Equal("UTC");
        response.Headers.GetValues("X-ClickHouse-Server-Display-Name").Should().Equal("inmemory");
        response.Headers.Contains("X-ClickHouse-Query-Id").Should().BeTrue();
    }

    [Fact]
    public async Task Select_returns_rowbinary_with_names_and_types_by_default()
    {
        var response = await Post("SELECT 1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Should().Equal([0x01, 0x01, (byte)'1', 0x05, .. "UInt8"u8.ToArray(), 0x01]);
        response.Headers.GetValues("X-ClickHouse-Format").Should().Equal("RowBinaryWithNamesAndTypes");
    }

    [Fact]
    public async Task Insert_returns_an_empty_body_and_the_summary_header()
    {
        var response = await Post(
            "INSERT INTO order_timings (timing_id, order_id, station, item_type, prep_seconds, recorded_at) " +
            "VALUES ({t:String}, {o:String}, {s:String}, {i:String}, {p:Float64}, {r:DateTime})",
            query: "&param_t=id-1&param_o=o-1&param_s=Griddle&param_i=Pancakes&param_p=2.5&param_r=2026-09-02T19%3a57%3a52");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsByteArrayAsync()).Should().BeEmpty();
        response.Headers.GetValues("X-ClickHouse-Summary").Single().Should().Contain("\"written_rows\":\"1\"");

        var stored = _engine.Execute("SELECT recorded_at FROM order_timings WHERE timing_id = {t:String}", Params(("t", "id-1")));
        stored.Rows.Single()[0].Should().Be(new DateTime(2026, 9, 2, 19, 57, 52, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Gzip_request_bodies_are_decompressed()
    {
        using var compressed = new MemoryStream();
        await using (var gzip = new GZipStream(compressed, CompressionMode.Compress, leaveOpen: true))
            await gzip.WriteAsync(Encoding.UTF8.GetBytes("SELECT 1"));

        var content = new ByteArrayContent(compressed.ToArray());
        content.Headers.ContentEncoding.Add("gzip");
        content.Headers.ContentType = new("text/sql");

        var response = await _client.PostAsync(BaseUrl, content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsByteArrayAsync()).Should().EndWith([0x01]);
    }

    [Fact]
    public async Task Sql_can_come_from_the_query_string_and_a_trailing_format_clause_wins_over_default_format()
    {
        var response = await _client.PostAsync(BaseUrl + "&query=SELECT%201%20FORMAT%20TSV", new ByteArrayContent([]));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("1\n");
    }

    [Theory]
    [InlineData("SELEC 1", HttpStatusCode.BadRequest, 62, "SYNTAX_ERROR")]
    [InlineData("SELECT * FROM nope", HttpStatusCode.NotFound, 60, "UNKNOWN_TABLE")]
    [InlineData("SELECT BAD SYNTAX", HttpStatusCode.NotFound, 47, "UNKNOWN_IDENTIFIER")]
    [InlineData("SELECT * FROM order_timings FINAL", HttpStatusCode.NotImplemented, 48, "NOT_IMPLEMENTED")]
    public async Task Errors_use_the_servers_status_mapping_header_and_body_format(string sql, HttpStatusCode status, int code, string name)
    {
        var response = await Post(sql);

        response.StatusCode.Should().Be(status);
        response.Headers.GetValues("X-ClickHouse-Exception-Code").Should().Equal(code.ToString());
        var body = await response.Content.ReadAsStringAsync();
        body.Should().StartWith($"Code: {code}. DB::Exception: ");
        body.Should().EndWith($". ({name}) (version 25.8.33.6 (official build))");
    }

    [Fact]
    public async Task Unsupported_output_formats_are_refused()
    {
        var response = await Post("SELECT 1 FORMAT JSONEachRow");

        response.StatusCode.Should().Be(HttpStatusCode.NotImplemented);
        (await response.Content.ReadAsStringAsync()).Should().Contain("JSONEachRow");
    }

    [Fact]
    public async Task Ddl_over_http_is_accepted()
    {
        var response = await Post("CREATE TABLE IF NOT EXISTS extra (id String) ENGINE = MergeTree() ORDER BY id");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _engine.Execute("SHOW TABLES", NoParams).Rows.Select(r => r[0]).Should().Contain("extra");
    }

    private Task<HttpResponseMessage> Post(string sql, string contentType = "text/sql", string query = "")
    {
        var content = new StringContent(sql, Encoding.UTF8, contentType);
        return _client.PostAsync(BaseUrl + query, content);
    }
}
