using System.Net.Http.Json;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Inventory;
using BreakfastProvider.Tests.Component.Shared.Util;

namespace BreakfastProvider.Tests.Component.Shared.Common.Inventory;

public class PostInventorySteps(RequestContext context)
{
    public TestInventoryItemRequest Request { get; set; } = new();
    public HttpResponseMessage? ResponseMessage { get; private set; }
    public TestInventoryItemResponse? Response { get; private set; }

    public async Task Send()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Endpoints.Inventory)
        {
            Content = JsonContent.Create(Request)
        };
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task ParseResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        Json.IsValid(content).Should().BeTrue();
        Response = Json.Deserialize<TestInventoryItemResponse>(content)!;
    }
}
