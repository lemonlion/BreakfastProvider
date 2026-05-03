using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Inventory;
using BreakfastProvider.Tests.Component.Shared.Util;

namespace BreakfastProvider.Tests.Component.Shared.Common.Inventory;

public class GetInventorySteps(RequestContext context)
{
    public HttpResponseMessage? ResponseMessage { get; private set; }
    public TestInventoryItemResponse? Response { get; private set; }
    public List<TestInventoryItemResponse>? ListResponse { get; private set; }

    public async Task RetrieveById(int id)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Endpoints.Inventory}/{id}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task RetrieveAll()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, Endpoints.Inventory);
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task ParseResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        var responseContentIsValidJson = Json.IsValid(content);
        Track.That(() => responseContentIsValidJson.Should().BeTrue());
        Response = Json.Deserialize<TestInventoryItemResponse>(content)!;
    }

    public async Task ParseListResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        var responseContentIsValidJson = Json.IsValid(content);
        Track.That(() => responseContentIsValidJson.Should().BeTrue());
        ListResponse = Json.Deserialize<List<TestInventoryItemResponse>>(content)!;
    }
}
