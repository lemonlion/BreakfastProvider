using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.OrderTimings;
using BreakfastProvider.Tests.Component.Shared.Util;

namespace BreakfastProvider.Tests.Component.Shared.Common.OrderTimings;

public class GetOrderTimingSteps(RequestContext context)
{
    public HttpResponseMessage? ResponseMessage { get; private set; }
    public List<TestOrderTimingResponse>? ListResponse { get; private set; }
    public List<TestOrderTimingSummaryResponse>? SummaryResponse { get; private set; }

    public async Task RetrieveSummary()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Endpoints.OrderTimings}/summary");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task RetrieveByStation(string station)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Endpoints.OrderTimings}/station/{station}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task ParseListResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        var responseContentIsValidJson = Json.IsValid(content);
        responseContentIsValidJson.Should().BeTrue();
        ListResponse = Json.Deserialize<List<TestOrderTimingResponse>>(content)!;
    }

    public async Task ParseSummaryResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        var responseContentIsValidJson = Json.IsValid(content);
        responseContentIsValidJson.Should().BeTrue();
        SummaryResponse = Json.Deserialize<List<TestOrderTimingSummaryResponse>>(content)!;
    }
}
