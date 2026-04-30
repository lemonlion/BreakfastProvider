using System.Net.Http.Json;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Ingredients;

namespace BreakfastProvider.Tests.Component.Shared.Common.Ingredients;

public class GetFlourSteps(RequestContext context)
{
    public HttpResponseMessage? ResponseMessage { get; private set; }
    public TestFlourResponse FlourResponse { get; private set; } = new();

    public async Task Retrieve()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, Endpoints.Flour);
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
        FlourResponse = (await ResponseMessage.Content.ReadFromJsonAsync<TestFlourResponse>())!;
    }
}
