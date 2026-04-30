using System.Net.Http.Json;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Util;

namespace BreakfastProvider.Tests.Component.Shared.Common.Orders;

public class PostOrderSteps(RequestContext context)
{
    private readonly Dictionary<string, string> _extraHeaders = new();

    public TestOrderRequest Request { get; set; } = new();
    public HttpResponseMessage? ResponseMessage { get; private set; }
    public TestOrderResponse? Response { get; private set; }

    public void AddHeader(string name, string value) => _extraHeaders[name] = value;

    public async Task Send()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Endpoints.Orders)
        {
            Content = JsonContent.Create(Request)
        };
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        foreach (var header in _extraHeaders)
            request.Headers.Add(header.Key, header.Value);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task ParseResponse()
    {
        var responseString = await ResponseMessage!.Content.ReadAsStringAsync();
        Json.IsValid(responseString).Should().BeTrue();
        Response = Json.Deserialize<TestOrderResponse>(responseString)!;
    }
}
