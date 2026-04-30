using BreakfastProvider.Tests.Component.Shared.Constants;

namespace BreakfastProvider.Tests.Component.Shared.Common.Inventory;

public class DeleteInventorySteps(RequestContext context)
{
    public HttpResponseMessage? ResponseMessage { get; private set; }

    public async Task Send(int id)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{Endpoints.Inventory}/{id}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }
}
