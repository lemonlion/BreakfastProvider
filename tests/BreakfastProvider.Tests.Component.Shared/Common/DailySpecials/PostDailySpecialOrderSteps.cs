using System.Net.Http.Json;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.DailySpecials;
using BreakfastProvider.Tests.Component.Shared.Util;

namespace BreakfastProvider.Tests.Component.Shared.Common.DailySpecials;

public class PostDailySpecialOrderSteps(RequestContext context)
{
    public TestDailySpecialOrderRequest Request { get; set; } = new();
    public HttpResponseMessage? ResponseMessage { get; private set; }
    public TestDailySpecialOrderResponse? Response { get; private set; }

    private readonly Dictionary<string, string> _additionalHeaders = new();

    public void AddHeader(string name, string value) => _additionalHeaders[name] = value;

    public async Task Send()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Endpoints.DailySpecialsOrders)
        {
            Content = JsonContent.Create(Request)
        };
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        foreach (var header in _additionalHeaders)
            request.Headers.Add(header.Key, header.Value);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task ParseResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        Json.IsValid(content).Should().BeTrue();
        Response = Json.Deserialize<TestDailySpecialOrderResponse>(content)!;
    }
}
