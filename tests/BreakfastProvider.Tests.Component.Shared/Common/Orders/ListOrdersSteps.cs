using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Util;

namespace BreakfastProvider.Tests.Component.Shared.Common.Orders;

public class ListOrdersSteps(RequestContext context)
{
    public HttpResponseMessage? ResponseMessage { get; private set; }
    public TestPaginatedOrderResponse? Response { get; private set; }

    public async Task Retrieve(int page = 1, int pageSize = 10)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Endpoints.Orders}?page={page}&pageSize={pageSize}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task ParseResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        Track.That(() => Json.IsValid(content).Should().BeTrue());
        Response = Json.Deserialize<TestPaginatedOrderResponse>(content)!;
    }
}
