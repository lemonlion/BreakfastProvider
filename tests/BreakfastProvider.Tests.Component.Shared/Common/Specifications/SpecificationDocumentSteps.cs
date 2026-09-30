using System.Text.Json;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Util;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace BreakfastProvider.Tests.Component.Shared.Common.Specifications;

/// <summary>
/// Fetches a contract the service publishes — a document or the page that renders it — and writes it into docs/.
/// </summary>
public class SpecificationDocumentSteps(RequestContext context)
{
    private Lazy<FileDescriptorSet?> _descriptorSet = new(() => null);

    public HttpResponseMessage? ResponseMessage { get; private set; }
    public string? Body { get; private set; }

    /// <summary>The body parsed as JSON, or null when it is not JSON.</summary>
    public JsonDocument? Json { get; private set; }

    /// <summary>
    /// The body parsed as a protobuf FileDescriptorSet in protobuf's JSON mapping — the gRPC contract document — or
    /// null when it is not one. The parser rejects unknown fields, so this checks more than that the body is JSON.
    /// </summary>
    public FileDescriptorSet? DescriptorSet => _descriptorSet.Value;

    public async Task Retrieve(string path, string? accept = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        if (accept is not null)
            request.Headers.Accept.ParseAdd(accept);
        ResponseMessage = await context.Client.SendAsync(request);
        var body = await ResponseMessage.Content.ReadAsStringAsync();
        Body = body;
        Json = Util.Json.TryParse(body, out var json) ? json : null;
        _descriptorSet = new Lazy<FileDescriptorSet?>(() => ParseDescriptorSet(body));
    }

    /// <summary>
    /// Writes the fetched body into docs/ and returns its full path, for the suite to attach. Only a document that was
    /// fetched successfully is written: BDDfy runs the steps after a failed assertion too, and a failed fetch must not
    /// replace a contract in docs/.
    /// </summary>
    public Task<string> WriteToDocs(string fileName)
    {
        if (ResponseMessage is not { IsSuccessStatusCode: true })
            throw new InvalidOperationException(
                $"{fileName} was not fetched successfully ({(int?)ResponseMessage?.StatusCode}), so docs/ is left as it is.");
        return ContractDocs.WriteAsync(fileName, Body!);
    }

    private static FileDescriptorSet? ParseDescriptorSet(string body)
    {
        try
        {
            return JsonParser.Default.Parse<FileDescriptorSet>(body);
        }
        catch (Exception exception) when (exception is InvalidProtocolBufferException or InvalidJsonException)
        {
            return null;
        }
    }
}
