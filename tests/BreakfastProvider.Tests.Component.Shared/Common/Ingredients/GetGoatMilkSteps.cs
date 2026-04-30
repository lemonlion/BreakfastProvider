using System.Net.Http.Json;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Ingredients;

namespace BreakfastProvider.Tests.Component.Shared.Common.Ingredients;

public class GetGoatMilkSteps(RequestContext context)
{
    private readonly Dictionary<string, string> _extraHeaders = new();

    public HttpResponseMessage? ResponseMessage { get; private set; }
    public TestGoatMilkResponse GoatMilkResponse { get; private set; } = new();

    public void AddHeader(string name, string value) => _extraHeaders[name] = value;

    public async Task Retrieve()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, Endpoints.GoatMilk);
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        foreach (var header in _extraHeaders)
            request.Headers.Add(header.Key, header.Value);
        ResponseMessage = await context.Client.SendAsync(request);
        if (ResponseMessage.IsSuccessStatusCode)
            GoatMilkResponse = (await ResponseMessage.Content.ReadFromJsonAsync<TestGoatMilkResponse>())!;
    }
}
