using System.Text.Json;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Util;

namespace BreakfastProvider.Tests.Component.Shared.Common.Specifications;

/// <summary>
/// Fetches a contract the service publishes — a document or the page that renders it — and writes it into docs/.
/// </summary>
public class SpecificationDocumentSteps(RequestContext context)
{
    public HttpResponseMessage? ResponseMessage { get; private set; }
    public string? Body { get; private set; }

    /// <summary>The body parsed as JSON, or null when it is not JSON.</summary>
    public JsonDocument? Json { get; private set; }

    public async Task Retrieve(string path, string? accept = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        if (accept is not null)
            request.Headers.Accept.ParseAdd(accept);
        ResponseMessage = await context.Client.SendAsync(request);
        Body = await ResponseMessage.Content.ReadAsStringAsync();
        Json = Util.Json.TryParse(Body, out var json) ? json : null;
    }

    /// <summary>Writes the fetched body into docs/ and returns its full path, for the suite to attach.</summary>
    public Task<string> WriteToDocs(string fileName) => ContractDocs.WriteAsync(fileName, Body!);
}
