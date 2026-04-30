using BreakfastProvider.Tests.Component.Shared.Constants;

namespace BreakfastProvider.Tests.Component.Shared.Common.Toppings;

public class DeleteToppingSteps(RequestContext context)
{
    public HttpResponseMessage? ResponseMessage { get; private set; }

    public async Task Send(Guid toppingId)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{Endpoints.Toppings}/{toppingId}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }
}
