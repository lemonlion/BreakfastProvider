using System.Net.Http.Json;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Util;

namespace BreakfastProvider.Tests.Component.Shared.Common.Orders;

public class PatchOrderStatusSteps(RequestContext context)
{
    public HttpResponseMessage? ResponseMessage { get; private set; }
    public TestOrderResponse? Response { get; private set; }

    public async Task Send(Guid orderId, string status)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"{Endpoints.Orders}/{orderId}/status")
        {
            Content = JsonContent.Create(new TestUpdateOrderStatusRequest { Status = status })
        };
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task ParseResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        var responseContentIsValidJson = Json.IsValid(content);
        Track.That(() => responseContentIsValidJson.Should().BeTrue());
        Response = Json.Deserialize<TestOrderResponse>(content)!;
    }
}
