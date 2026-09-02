using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using InMemoryEmulator.ClickHouse.Core;

namespace InMemoryEmulator.ClickHouse.Http;

/// <summary>
/// An <see cref="HttpMessageHandler"/> that speaks enough of the ClickHouse HTTP interface for
/// <c>ClickHouse.Client</c> to open a connection and run parameterised SELECT / INSERT / DELETE
/// statements against an <see cref="IClickHouseQueryEngine"/>. No ports, no lifecycle: hand it to an
/// <see cref="HttpClient"/> and hand that to the driver.
/// </summary>
public sealed partial class InMemoryClickHouseHandler(IClickHouseQueryEngine engine) : HttpMessageHandler
{
    /// <summary>Reported by the handshake; matches the Docker image so <c>ServerVersion</c> reads the same in every lane.</summary>
    public const string ServerVersion = "25.8.33.6";
    public const string Timezone = "UTC";
    public const string DisplayName = "inmemory";

    [GeneratedRegex(@"^\s*SELECT\s+version\(\)\s*,\s*timezone\(\)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex HandshakeRegex();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ClickHouseHttpRequest parsed;
        try
        {
            parsed = await ClickHouseHttpRequest.ParseAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            return ErrorResponse(ClickHouseEmulatorException.Generic($"Cannot parse request: {ex.Message}", ex));
        }

        try
        {
            if (HandshakeRegex().IsMatch(parsed.Sql))
            {
                var handshake = new ClickHouseResultSet(
                    [new ClickHouseColumn("version()", "String"), new ClickHouseColumn("timezone()", "String")],
                    [[ServerVersion, Timezone]]);
                return ResultResponse(handshake, parsed.Format);
            }

            if (ClickHouseSqlTranslator.ReturnsResultSet(parsed.Sql))
            {
                var resultSet = engine.Execute(parsed.Sql, parsed.Parameters);
                return ResultResponse(resultSet, parsed.Format);
            }

            var rowsAffected = engine.ExecuteNonQuery(parsed.Sql, parsed.Parameters);
            return NonQueryResponse(rowsAffected);
        }
        catch (ClickHouseEmulatorException ex)
        {
            return ErrorResponse(ex);
        }
        catch (NotSupportedException ex)
        {
            return ErrorResponse(ClickHouseEmulatorException.NotImplemented(ex.Message, ex));
        }
    }

    private static HttpResponseMessage ResultResponse(ClickHouseResultSet resultSet, string format)
    {
        HttpContent content;
        switch (format)
        {
            case "RowBinaryWithNamesAndTypes":
                content = new ByteArrayContent(RowBinaryWriter.WriteWithNamesAndTypes(resultSet));
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                break;
            case "TSV":
            case "TabSeparated":
                content = TsvContent(TsvWriter.Write(resultSet));
                break;
            case "TSVWithNamesAndTypes":
            case "TabSeparatedWithNamesAndTypes":
                content = TsvContent(TsvWriter.Write(resultSet, withNamesAndTypes: true));
                break;
            default:
                return ErrorResponse(ClickHouseEmulatorException.NotImplemented(
                    $"The in-memory ClickHouse emulator does not support output format '{format}'"));
        }

        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        AddCommonHeaders(response, format);
        return response;
    }

    private static HttpResponseMessage NonQueryResponse(int rowsAffected)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([]) };
        AddCommonHeaders(response, null);

        // String-valued numbers, exactly as the server emits them; only feeds the driver's QueryStats.
        response.Headers.TryAddWithoutValidation("X-ClickHouse-Summary",
            "{\"read_rows\":\"0\",\"read_bytes\":\"0\",\"written_rows\":\"" + rowsAffected +
            "\",\"written_bytes\":\"0\",\"total_rows_to_read\":\"0\",\"result_rows\":\"0\",\"result_bytes\":\"0\",\"elapsed_ns\":\"0\"}");
        return response;
    }

    private static HttpResponseMessage ErrorResponse(ClickHouseEmulatorException exception)
    {
        // The server's own mapping: SYNTAX_ERROR → 400; UNKNOWN_TABLE / UNKNOWN_IDENTIFIER → 404;
        // NOT_IMPLEMENTED → 501; everything else → 500.
        var status = exception.Code switch
        {
            62 => HttpStatusCode.BadRequest,
            60 or 47 => HttpStatusCode.NotFound,
            48 => HttpStatusCode.NotImplemented,
            _ => HttpStatusCode.InternalServerError
        };

        var message = exception.Message.TrimEnd().TrimEnd('.');
        var body = $"Code: {exception.Code}. DB::Exception: {message}. ({exception.Name}) (version {ServerVersion} (official build))";

        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "text/plain")
        };
        AddCommonHeaders(response, null);
        response.Headers.TryAddWithoutValidation("X-ClickHouse-Exception-Code", exception.Code.ToString());
        return response;
    }

    private static void AddCommonHeaders(HttpResponseMessage response, string? format)
    {
        response.Headers.TryAddWithoutValidation("X-ClickHouse-Query-Id", Guid.NewGuid().ToString());
        response.Headers.TryAddWithoutValidation("X-ClickHouse-Timezone", Timezone);
        response.Headers.TryAddWithoutValidation("X-ClickHouse-Server-Display-Name", DisplayName);
        if (format is not null)
            response.Headers.TryAddWithoutValidation("X-ClickHouse-Format", format);
    }

    private static StringContent TsvContent(string text) =>
        new(text, Encoding.UTF8, "text/tab-separated-values");
}
